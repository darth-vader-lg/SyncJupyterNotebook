using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

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
//         var ipynb = File.ReadAllText(opt.Path);
//         var result = new StringBuilder();
//         var ix = 0;
//         var c = '\0';
//         var col = 0;
//         var row = 0;
//#if DEBUG
//         var currentLinePosition = ipynb;
//#endif
//         // Legge un carattere e lo memorizza in c
//         char GetChar(bool skip)
//         {
//            if (ix >= ipynb.Length)
//               return c = '\0';
//            c = ipynb[ix++];
//            if (!skip)
//               result.Append(c);
//            if (c == '\n') {
//               row++;
//               col = 0;
//            }
//#if DEBUG
//            currentLinePosition = ipynb[ix..];
//#endif
//            return c;
//         }
//         void SkipSeparators(bool skip)
//         {
//            if (ix >= ipynb.Length)
//               return;
//            while ((c = ipynb[ix]) == ' ' || c == '\t' || c == '\r' || c == '\n') {
//               if (GetChar(skip) == '\0')
//                  break;
//            }
//         }
//         // Apre una parte fra delimitatori
//         bool OpenDelims(bool skip, string brackets, Func<bool> execute)
//         {
//            if (brackets.Length > 1)
//               while (GetChar(skip) != brackets[0] && c != '\0') ;
//            SkipSeparators(skip);
//            if (!execute())
//               return false;
//            SkipSeparators(skip);
//            return GetChar(skip) == brackets[^1];
//         }
//         // Cerca una chiave
//         bool GetKey(bool skip, Func<string, bool> execute)
//         {
//            SkipSeparators(skip);
//            if (c != '\"')
//               return false;
//            GetChar(skip);
//            var name = new StringBuilder();
//            while (GetChar(skip) != '\"')
//               name.Append(c);
//            SkipSeparators(skip);
//            if (GetChar(skip) != ':')
//               throw new Exception($"Invalid key declaration at row {row}, col {col}");
//            if (!execute(name.ToString()))
//               return false;
//            return true;
//         }
//         bool GetValue(bool skip)
//         {
//            SkipSeparators(skip);
//            switch (c) {
//               case '{':
//                  if (!OpenDelims(skip, "{}", () =>
//                  {
//                     while (GetKey(skip, k => GetValue(skip))) ;
//                     return true;
//                  }))
//                     return false;
//                  break;
//               case '[':
//                  if (!OpenDelims(skip, "[]", () =>
//                  {
//                     while (GetValue(skip) && c == ',') ;
//                     return true;
//                  }))
//                     return false;
//                  break;
//               case '\"':
//                  if (!OpenDelims(skip, "\"\"", () =>
//                  {
//                     var cPrev = '\0';
//                     while (ipynb[ix] != '\"' || cPrev == '\\')
//                        cPrev = GetChar(skip);
//                     return true;
//                  }))
//                     return false;
//                  break;
//               default:
//                  while (ix < ipynb.Length && (c = ipynb[ix]) != ',' && c != '}' && c != ']')
//                     GetChar(skip);
//                  break;
//            }
//            SkipSeparators(skip);
//            if (c == ',')
//               GetChar(skip);
//            return ix < ipynb.Length;
//         }
         //OpenDelims(false, "{}", () =>
         //{
         //   while (GetKey(false, key =>
         //   {
         //      if (key == "cells") {
         //         while (OpenDelims(false, "[]", () =>
         //         {
         //            while (OpenDelims(false, "{}", () =>
         //            {
         //               while (GetKey(false, key =>
         //               {
         //                  if (key == "source")
         //                     return GetValue(false);
         //                  return GetValue(false);
         //               })) ;
         //               return true;
         //            }));
         //            return true;
         //         }));
         //         return true;
         //      }
         //      return GetValue(false);
         //   }));
         //   return true;
         //});

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
