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
      /// Funzione di sincronizzazione del notebook
      /// </summary>
      /// <param name="opt">Opzioni</param>
      static private int RunSyncNotebook(OptionsSyncNotebook opt)
      {
         if (!File.Exists(opt.Path)) {
            Console.Error.WriteLine($"Error: the notebook {opt.Path} doesn't exist!");
            return 1;
         }
         var root = NotebookParser.Parse(File.ReadAllText(opt.Path));
         foreach (var cell in root["cells"]) {
            foreach (var line in cell["source"]) {
               Console.WriteLine(line.Value);
            }
         }
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
         return 0;
      }
      #endregion
   }

   partial class Program // OptionsSyncNotebook
   {
      /// <summary>
      /// Opzioni di sincronizzazione Python -> Notebook
      /// </summary>
      [Verb("nb", HelpText = "Syncronize a notebook with the related python files.")]
      class OptionsSyncNotebook
      {
         #region Properties
         /// <summary>
         /// Path della directory radice dei repositories
         /// </summary>
         [Value(0, Default = ".", HelpText = "The path of notebook.")]
         public string Path { get; set; }
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
      /// Opzioni di sincronizzazione Python -> Notebook
      /// </summary>
      [Verb("py", HelpText = "Syncronize the python files with the content of a notebook.")]
      class OptionsSyncPython
      {
         #region Properties
         /// <summary>
         /// Path della directory radice dei repositories
         /// </summary>
         [Value(0, Default = ".", HelpText = "The path of notebook.")]
         public string Path { get; set; }
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
