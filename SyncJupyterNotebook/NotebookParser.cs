using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;

namespace SyncJupyterNotebook
{
   /// <summary>
   /// Parser di file ipynb
   /// </summary>
   partial class NotebookParser
   {
      #region Fields
      /// <summary>
      /// Ultimo carattere letto
      /// </summary>
      private char c;
      /// <summary>
      /// Colonna attuale
      /// </summary>
      private int col;
#if DEBUG
      /// <summary>
      /// Posizione corrente di parsing
      /// </summary>
      string currentLinePosition;
#endif
      /// <summary>
      /// File ipynb
      /// </summary>
      private readonly string ipynb;
      /// <summary>
      /// Indice attuale nel testo
      /// </summary>
      private int ix;
      /// <summary>
      /// Riga attuale
      /// </summary>
      private int row;
      #endregion
      #region Delegates
      /// <summary>
      /// Delegato di parserizzazione del valore di un elemento
      /// </summary>
      /// <param name="item">Item corrente</param>
      /// <returns>true se la parserizzazione e' avvenuta correttamente</returns>
      private delegate bool ParseItemValue(Item item);
      /// <summary>
      /// Delegato di parserizzazione del contenuto di una sezione
      /// </summary>
      /// <param name="item">Item corrente</param>
      /// <returns>true se la parserizzazione e' avvenuta correttamente</returns>
      private delegate bool ParseSectionContent(Item item);
      #endregion
      #region Methods
      /// <summary>
      /// Costruttore
      /// </summary>
      /// <param name="text">Testo</param>
      private NotebookParser(string text)
      {
         ipynb = text;
#if DEBUG
         currentLinePosition = ipynb;
#endif
      }
      /// <summary>
      /// Legge un carattere e lo memorizza in c
      /// </summary>
      /// <returns>Il carattere letto</returns>
      private char GetChar()
      {
         if (ix >= ipynb.Length)
            return c = '\0';
         c = ipynb[ix++];
         if (c == '\n') {
            row++;
            col = 0;
         }
#if DEBUG
         currentLinePosition = ipynb[ix..];
#endif
         return c;
      }
      /// <summary>
      /// Parserizza un testo
      /// </summary>
      /// <param name="text"></param>
      /// <returns></returns>
      internal static Item Parse(string text)
      {
         var parser = new NotebookParser(text);
         var root = new Item(null);
         parser.ParseValue(root);
         return root;
      }
      /// <summary>
      /// Parserizza un elemento
      /// </summary>
      /// <param name="item">Item corrente</param>
      /// <param name="parseItemValue">Funzione di parsing del valore di un elemento</param>
      /// <returns></returns>
      private bool ParseItem(Item item, ParseItemValue parseItemValue)
      {
         SkipSeparators();
         if (c != '\"')
            return false;
         GetChar();
         var name = new StringBuilder();
         while (GetChar() != '\"')
            name.Append(c);
         SkipSeparators();
         if (GetChar() != ':')
            throw new Exception($"Invalid key declaration at row {row}, col {col}");
         var childItem = new Item(name.ToString().Trim());
         if (!parseItemValue(childItem))
            return false;
         item.Add(childItem);
         return true;
      }
      /// <summary>
      /// Parserizza una sezione
      /// </summary>
      /// <param name="item">Item corrente</param>
      /// <param name="brackets">Tipi di parentesi</param>
      /// <param name="parseSectionContent">Funzione di parsing del contenuto della sezione</param>
      /// <returns>true se parsing avvenuto correttamente</returns>
      private bool ParseSection(Item item, string brackets, ParseSectionContent parseSectionContent)
      {
         if (brackets.Length > 1)
            while (GetChar() != brackets[0] && c != '\0') ;
         if (brackets[0] != '\"')
            SkipSeparators();
         if (!parseSectionContent(item))
            return false;
         SkipSeparators();
         return GetChar() == brackets[^1];
      }
      /// <summary>
      /// Parserizza un valore
      /// </summary>
      /// <param name="item">Item corrente</param>
      /// <returns>true se parsing avvenuto correttamente</returns>
      private bool ParseValue(Item item)
      {
         SkipSeparators();
         switch (c) {
            case '{':
               if (!ParseSection(item, "{}", sectionItem =>
               {
                  while (ParseItem(sectionItem, valueItem => ParseValue(valueItem))) ;
                  return true;
               }))
                  return false;
               break;
            case '[':
               if (!ParseSection(item, "[]", sectionItem =>
               {
                  var arrayItem = new Item(null);
                  while (ParseValue(arrayItem)) {
                     if (arrayItem.Count > 0 || !string.IsNullOrEmpty(arrayItem.Value))
                        sectionItem.Add(arrayItem);
                     if (c != ',')
                        break;
                     arrayItem = new Item(null);
                  }
                  return true;
               }))
                  return false;
               break;
            case '\"':
               if (!ParseSection(item, "\"\"", valueItem =>
               {
                  var cPrev = '\0';
                  var value = new StringBuilder();
                  value.Append('"');
                  while (ipynb[ix] != '\"' || cPrev == '\\')
                     value.Append(cPrev = GetChar());
                  value.Append('"');
                  item.Value = value.ToString();
                  return true;
               }))
                  return false;
               break;
            default:
               var value = new StringBuilder();
               while (ix < ipynb.Length && (c = ipynb[ix]) != ',' && c != '}' && c != ']')
                  value.Append(GetChar());
               item.Value = value.ToString().Trim();
               break;
         }
         SkipSeparators();
         if (c == ',')
            GetChar();
         return ix < ipynb.Length;
      }
      /// <summary>
      /// Salta i separatori
      /// </summary>
      private void SkipSeparators()
      {
         if (ix >= ipynb.Length)
            return;
         while ((c = ipynb[ix]) == ' ' || c == '\t' || c == '\r' || c == '\n') {
            if (GetChar() == '\0')
               break;
         }
      }
      #endregion
   }

   partial class NotebookParser // Item
   {
      /// <summary>
      /// Item del file
      /// </summary>
      [DebuggerDisplay("{DebuggerDisplay,nq}")]
      internal class Item : KeyedCollection<string, Item>
      {
         #region Properties
         /// <summary>
         /// Funzione di visualizzazione di debug
         /// </summary>
         private string DebuggerDisplay => $"{Name}{(Count > 0 ? $", [{Count}]" : "")}{(Value != null ? $", {Value}" : "")}";
         /// <summary>
         /// Nome dell'item
         /// </summary>
         public string Name { get; private set; }
         /// <summary>
         /// Valore
         /// </summary>
         public string Value { get; set; }
         #endregion
         #region Methods
         /// <summary>
         /// Costruttore
         /// </summary>
         /// <param name="name">Nome dell'item</param>
         /// <param name="value">Eventuale valore</param>
         public Item(string name, string value = null)
         {
            Name = name;
            Value = value;
         }
         /// <summary>
         /// Restituisce la chiave per l'item
         /// </summary>
         /// <param name="item">L'item</param>
         /// <returns>La chiave</returns>
         protected override string GetKeyForItem(Item item)
         {
            if (!string.IsNullOrEmpty(item.Name))
               return item.Name;
            for (var i = 0; i < Count; i++) {
               if (this[i] == item)
                  return $"[{i}]";
            }
            var newKey = $"[{Count}]";
            return newKey;
         }
         /// <summary>
         /// Conversione a stringa
         /// </summary>
         /// <returns>La rappresentazione in stringa</returns>
         public override string ToString() => ToString(0);
         /// <summary>
         /// Conversione a stringa
         /// </summary>
         /// <param name="col">Colonna di partenza</param>
         /// <param name="indent">Indentazione</param>
         /// <returns>La rappresentazione in stringa</returns>
         public string ToString(int col, int indent = 2)
         {
            var result = new StringBuilder();
            var strIndent = new string(' ', col);
            if (Count < 1 && !string.IsNullOrEmpty(Value))
               result.Append($"{strIndent}{(!string.IsNullOrEmpty(Name) ? $"\"{Name}\": " : "")}{Value}");
            if (Count > 0 || string.IsNullOrEmpty(Value)) {
               var brackets = Count > 0 && !string.IsNullOrEmpty(this[0].Name) ? "{}" : "[]";
               result.Append($"{strIndent}{(!string.IsNullOrEmpty(Name) ? $"\"{Name}\": " : "")}{brackets[0]}");
               if (Count == 0)
                  result.Append($"{brackets[1]}");
               else {
                  result.Append('\n');
                  for (var i = 0; i < Count; i++)
                     result.Append($"{this[i].ToString(col + indent)}{(i == Count - 1 ? "\n" : ",\n")}");
                  result.Append($"{strIndent}{brackets[1]}");
               }
            }
            return result.ToString();
         }
         #endregion
      }
   }
}
