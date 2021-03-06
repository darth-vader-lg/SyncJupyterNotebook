using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.IO;

namespace SyncJupyterNotebook
{
   /// <summary>
   /// Programma
   /// </summary>
   partial class Program
   {
      #region Methods
      /// <summary>
      /// Entry point
      /// </summary>
      /// <param name="args">Argomenti</param>
      static void Main(string[] args)
      {
         Parser.Default.ParseArguments<OptionsSyncNotebook, OptionsSyncPython>(args)
            .MapResult(
            (OptionsSyncNotebook opt) => RunSyncNotebook(opt),
            (OptionsSyncPython opt) => RunSyncPython(opt),
            errors => 1);
      }
      /// <summary>
      /// Restituisce i blocchi contenenti moduli di testo all'interno di un sorgente di un notebook
      /// </summary>
      /// <param name="beginBlockMarker">Marcatore di inizio blocco</param>
      /// <param name="endBlockMarker">Marcatore di fine blocco</param>
      /// <param name="rootDir">Directory radice</param>
      /// <param name="source">Sorgente del notebook</param>
      /// <returns>L'array di blocchi di codice python</returns>
      static private (NotebookParser.Item start, NotebookParser.Item end, string pyFileName)[] GetBlocks(string beginBlockMarker, string endBlockMarker, string rootDir, NotebookParser.Item source)
      {
         // Loop su tutte le linee del sorgente
         var result = new List<(NotebookParser.Item start, NotebookParser.Item end, string pyFileName)>();
         for (var i = 0; i < source.Count; i++) {
            // Verifica se la linea contiene il marcatore di inzio file python
            var lineStart = source[i].Value;
            if (lineStart.Contains(beginBlockMarker)) {
               lineStart = lineStart[1..^1];
               if (lineStart.Trim().StartsWith("#")) {
                  var fileName = lineStart.Substring(lineStart.IndexOf(beginBlockMarker) + beginBlockMarker.Length).Trim();
                  fileName = fileName.Replace("\\n", "");
                  fileName = Path.Combine(rootDir, fileName);
                  // Cerca il marcatore di fine file python
                  var j = i + 1;
                  while (j < source.Count) {
                     var lineEnd = source[j].Value;
                     if (lineEnd.Contains(endBlockMarker)) {
                        if (lineEnd[1..^1].Trim().StartsWith("#"))
                           break;
                     }
                     j++;
                  }
                  result.Add((start: source[i], end: j < source.Count ? source[j] : null, pyFileName: fileName));
               }
            }
         }
         return result.ToArray();
      }
      /// <summary>
      /// Restituisce i blocchi contenenti moduli di codice python all'interno di un sorgente di un notebook
      /// </summary>
      /// <param name="opt">Opzioni</param>
      /// <param name="source">Sorgente del notebook</param>
      /// <returns>L'array di blocchi di codice python</returns>
      static private (NotebookParser.Item start, NotebookParser.Item end, string pyFileName)[] GetPythonBlocks(OptionsSyncBase opt, NotebookParser.Item source) =>
         GetBlocks(opt.BeginModule, opt.EndModule, Path.GetDirectoryName(Path.GetFullPath(opt.Path)), source);
      /// <summary>
      /// Restituisce i blocchi contenenti file di testo all'interno di un sorgente di un notebook
      /// </summary>
      /// <param name="opt">Opzioni</param>
      /// <param name="source">Sorgente del notebook</param>
      /// <returns>L'array di blocchi di testo</returns>
      static private (NotebookParser.Item start, NotebookParser.Item end, string txtFileName)[] GetTextBlocks(OptionsSyncBase opt, NotebookParser.Item source) =>
         GetBlocks(opt.BeginText, opt.EndText, Path.GetDirectoryName(Path.GetFullPath(opt.Path)), source);
      /// <summary>
      /// Funzione di sincronizzazione del notebook
      /// </summary>
      /// <param name="opt">Opzioni</param>
      static private int RunSyncNotebook(OptionsSyncNotebook opt)
      {
         // Verifica esistenza del file
         if (!File.Exists(opt.Path)) {
            Console.Error.WriteLine($"Error: the notebook {opt.Path} doesn't exist!");
            return 1;
         }
         // Parserizza il notebook
         var root = NotebookParser.Parse(File.ReadAllText(opt.Path));
         // Loop su tutte le celle
         foreach (var cell in root["cells"]) {
            // Sorgente
            var source = cell["source"];
            // Loop sui blocchi contenenti codice python
            var pyBlocks = GetPythonBlocks(opt, source);
            foreach (var block in pyBlocks) {
               // Verifica esistenza file python
               var pyFileName = Path.GetFullPath(block.pyFileName);
               if (!File.Exists(pyFileName)) {
                  Console.Error.WriteLine($"Warning: the file {pyFileName} doesn't exist!");
                  continue;
               }
               // Indice blocco di partenza
               var ix = source.IndexOf(block.start);
               if (ix < 0)
                  continue;
               ix++;
               // Elimina i blocchi dal notebook
               while (ix < source.Count && source[ix] != block.end)
                  source.RemoveAt(ix);
               // Inserisce il file python
               using var reader = new StreamReader(pyFileName);
               for (var pyLine = reader.ReadLine(); pyLine != null; pyLine = reader.ReadLine()) {
                  pyLine = pyLine.Replace("\\", "\\\\").Replace("\"", "\\\"");
                  source.Insert(ix++, new NotebookParser.Item(null, $"\"{pyLine}\\n\""));
               }
            }
            // Loop sui blocchi contenenti testo
            var txtBlocks = GetTextBlocks(opt, source);
            foreach (var block in txtBlocks) {
               // Verifica esistenza file di testo
               var txtFileName = Path.GetFullPath(block.txtFileName);
               if (!File.Exists(txtFileName)) {
                  Console.Error.WriteLine($"Warning: the file {txtFileName} doesn't exist!");
                  continue;
               }
               // Indice blocco di partenza
               var ix = source.IndexOf(block.start);
               if (ix < 0)
                  continue;
               ix++;
               // Elimina i blocchi dal notebook
               while (ix < source.Count && source[ix] != block.end)
                  source.RemoveAt(ix);
               // Inserisce il file python
               using var reader = new StreamReader(txtFileName);
               for (var pyLine = reader.ReadLine(); pyLine != null; pyLine = reader.ReadLine()) {
                  pyLine = pyLine.Replace("\\", "\\\\").Replace("\"", "\\\"");
                  source.Insert(ix++, new NotebookParser.Item(null, $"\"\'{pyLine}\'\\n\""));
               }
            }
         }
         var str = root.ToString();
         using var writer = new StreamWriter(opt.Path) { NewLine = "\n" };
         writer.Write(str);
         return 0;
      }
      /// <summary>
      /// Funzione di sincronizzazione dei files python
      /// </summary>
      /// <param name="opt">Opzioni</param>
      static private int RunSyncPython(OptionsSyncPython opt)
      {
         if (!File.Exists(opt.Path)) {
            Console.Error.WriteLine($"Error: the notebook {opt.Path} doesn't exist!");
            return 1;
         }
         // Parserizza il notebook
         var root = NotebookParser.Parse(File.ReadAllText(opt.Path));
         // Loop su tutte le celle
         foreach (var cell in root["cells"]) {
            // Sorgente
            var source = cell["source"];
            // Loop sui blocchi contenenti codice python
            var pyBlocks = GetPythonBlocks(opt, source);
            foreach (var block in pyBlocks) {
               // Verifica esistenza file python
               var pyFileName = Path.GetFullPath(block.pyFileName);
               if (!File.Exists(pyFileName))
                  Console.Error.WriteLine($"Warning: the file {pyFileName} doesn't exist. It will be created.");
               // Indice blocco di partenza
               var ix = source.IndexOf(block.start);
               if (ix < 0)
                  continue;
               ix++;
               // Crea la directory del file Python
               var pyFileDir = Path.GetDirectoryName(pyFileName);
               if (!Directory.Exists(pyFileDir))
                  Directory.CreateDirectory(pyFileDir);
               // Crea o sovrascrive il file python
               using var writer = new StreamWriter(pyFileName);
               // Crea i blocchi dal notebook
               while (ix < source.Count && source[ix] != block.end) {
                  var line = source[ix++].Value[1..^1].Replace("\\\\", "\\").Replace("\\\"", "\"").TrimEnd();
                  line = line.EndsWith("\\n") ? line[..^2] : line;
                  writer.WriteLine(line);
               }
            }
            // Loop sui blocchi contenenti testo
            var txtBlocks = GetTextBlocks(opt, source);
            foreach (var block in txtBlocks) {
               // Verifica esistenza file python
               var txtFileName = Path.GetFullPath(block.txtFileName);
               if (!File.Exists(txtFileName))
                  Console.Error.WriteLine($"Warning: the file {txtFileName} doesn't exist. It will be created.");
               // Indice blocco di partenza
               var ix = source.IndexOf(block.start);
               if (ix < 0)
                  continue;
               ix++;
               // Crea la directory del file di testo
               var pyFileDir = Path.GetDirectoryName(txtFileName);
               if (!Directory.Exists(pyFileDir))
                  Directory.CreateDirectory(pyFileDir);
               // Crea o sovrascrive il file di testo
               using var writer = new StreamWriter(txtFileName);

               // Crea le linee dal notebook
               while (ix < source.Count && source[ix] != block.end) {
                  var line = source[ix++].Value[1..^1].Replace("\\\\", "\\").Replace("\\\"", "\"").TrimEnd();
                  line = line.EndsWith("\\n") ? line[..^2] : line;
                  writer.WriteLine(line);
               }
            }
         }
         return 0;
      }
      #endregion
   }

   partial class Program // OptionsSyncBase
   {
      /// <summary>
      /// Opzioni di base
      /// </summary>
      class OptionsSyncBase
      {
         #region Properties
         /// <summary>
         /// Marcatore di inizio file python
         /// </summary>
         [Option('b', "begin-module", Default = "begin-module:", HelpText = "The marker for the begin of the python module referenced in the notebook.")]
         public string BeginModule { get; set; }
         /// <summary>
         /// Marcatore di inizio file di testo
         /// </summary>
         [Option('b', "begin-txt", Default = "begin-txt:", HelpText = "The marker for the begin of the text file referenced in the notebook.")]
         public string BeginText { get; set; }
         /// <summary>
         /// Marcatore di fine file  python
         /// </summary>
         [Option('e', "end-module", Default = "end-module", HelpText = "The marker for the end of the python module referenced in the notebook.")]
         public string EndModule { get; set; }
         /// <summary>
         /// Marcatore di fine file di testo
         /// </summary>
         [Option('e', "end-txt", Default = "end-txt", HelpText = "The marker for the end of the text file referenced in the notebook.")]
         public string EndText { get; set; }
         /// <summary>
         /// Path della directory radice dei repositories
         /// </summary>
         [Value(0, Default = ".", HelpText = "The path of notebook.")]
         public string Path { get; set; }
         #endregion
      }
   }

   partial class Program // OptionsSyncNotebook
   {
      /// <summary>
      /// Opzioni di sincronizzazione Python -> Notebook
      /// </summary>
      [Verb("nb", HelpText = "Syncronize a notebook with the related python files.")]
      class OptionsSyncNotebook : OptionsSyncBase
      {
         #region Properties
         /// <summary>
         /// Esempi di utilizzo
         /// </summary>
         [Usage]
         public static IEnumerable<Example> Examples
         {
            get => new List<Example>()
            {
               new("Syncronize a notebook with the related python files", new OptionsSyncNotebook { Path = @"path\to\the\notebook" }),
            };
         }
         #endregion
      }
   }

   partial class Program // OptionsSyncPython
   {
      /// <summary>
      /// Opzioni di sincronizzazione Notebook -> Python
      /// </summary>
      [Verb("py", HelpText = "Syncronize the python files with the content of a notebook.")]
      class OptionsSyncPython : OptionsSyncBase
      {
         #region Properties
         /// <summary>
         /// Esempi di utilizzo
         /// </summary>
         [Usage]
         public static IEnumerable<Example> Examples
         {
            get => new List<Example>()
            {
               new("Syncronize the python files with the content of a notebook", new OptionsSyncNotebook { Path = @"path\to\the\notebook" }),
            };
         }
         #endregion
      }
   }
}
