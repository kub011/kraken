﻿using Kraken.Tracing;
using Microsoft.SharePoint.Client;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kraken.SharePoint.Client.Caml {

  public static class CamlHelpers {

    public static class ViewFieldShorthandConstants {
      public const string Default = "default";
      public const string DefaultDocLib = "defaultdoclib";
      public const string DefaultLists = "defaultlist";
      public const string All = "all";
    }

    private static List<string> _defaultListViewFields = null;
    public static List<string> DefaultListViewFields {
      get {
        if (_defaultListViewFields == null) {
          _defaultListViewFields = new List<string>() {
            BuiltInFieldId.GetName(BuiltInFieldId.ID),
            BuiltInFieldId.GetName(BuiltInFieldId.URL),
            BuiltInFieldId.GetName(BuiltInFieldId.EncodedAbsUrl),
            BuiltInFieldId.GetName(BuiltInFieldId.Title),
            BuiltInFieldId.GetName(BuiltInFieldId.Created),
            BuiltInFieldId.GetName(BuiltInFieldId.Modified),
            BuiltInFieldId.GetName(BuiltInFieldId.Author),
            BuiltInFieldId.GetName(BuiltInFieldId.Editor),
            BuiltInFieldId.GetName(BuiltInFieldId.ContentTypeId),
            BuiltInFieldId.GetName(BuiltInFieldId.Last_x0020_Modified),
            BuiltInFieldId.GetName(BuiltInFieldId.Created_x0020_Date),
            // strictly speaking we don't really need the rest of these in all cases
            // but they are nice to have
            BuiltInFieldId.GetName(BuiltInFieldId._ModerationStatus),
            BuiltInFieldId.GetName(BuiltInFieldId.UniqueId),
            BuiltInFieldId.GetName(BuiltInFieldId.owshiddenversion),
            BuiltInFieldId.GetName(BuiltInFieldId.CheckoutUser),
            BuiltInFieldId.GetName(BuiltInFieldId.ProgId),
            BuiltInFieldId.GetName(BuiltInFieldId.MetaInfo)
          };
        }
        return _defaultListViewFields;
      }
    }

    private static List<string> _defaultDocLibViewFields = null;

    public static List<string> DefaultDocLibViewFields {
      get {
        if (_defaultDocLibViewFields == null) {
          _defaultDocLibViewFields = DefaultListViewFields;
          _defaultDocLibViewFields.AddRange(new string[] {
            BuiltInFieldId.GetName(BuiltInFieldId.FileRef),
            BuiltInFieldId.GetName(BuiltInFieldId.FSObjType),
            BuiltInFieldId.GetName(BuiltInFieldId._Level),
            BuiltInFieldId.GetName(BuiltInFieldId.File_x0020_Size),
            BuiltInFieldId.GetName(BuiltInFieldId.FileLeafRef),
            BuiltInFieldId.GetName(BuiltInFieldId.HTML_x0020_File_x0020_Type),
          });
        }
        return _defaultDocLibViewFields;
      }
    }

    public static List<string> GetDefaultQueryFields(this List list, ITrace trace = null) {
      if (trace == null) trace = NullTrace.Default;
      bool isDocLib = (list != null && list.IsDocumentLibrary(trace));
      List<string> viewFields = new List<string>();
      viewFields.Add(isDocLib ? ViewFieldShorthandConstants.DefaultDocLib : ViewFieldShorthandConstants.DefaultLists);
      return viewFields;
    }
    public static List<string> ResolveQueryFields(List<string> viewFields, List list = null, ITrace trace = null) {
      if (viewFields == null)
        viewFields = GetDefaultQueryFields(list, trace);
      if (viewFields.Count == 1) {
        if (string.Equals(viewFields[0], ViewFieldShorthandConstants.Default, StringComparison.InvariantCultureIgnoreCase))
          viewFields = DefaultDocLibViewFields;
        else if (string.Equals(viewFields[0], ViewFieldShorthandConstants.DefaultDocLib, StringComparison.InvariantCultureIgnoreCase))
          viewFields = DefaultDocLibViewFields;
        else if (string.Equals(viewFields[0], ViewFieldShorthandConstants.DefaultLists, StringComparison.InvariantCultureIgnoreCase))
          viewFields = DefaultDocLibViewFields;
      }
      return viewFields;
    }

    public static string GetCamlViewFieldsXml(this List<string> viewFields, List list = null, ITrace trace = null) {
      if (viewFields == null)
        viewFields = GetDefaultQueryFields(list, trace);
      else if (viewFields.Count == 1 && string.Equals(viewFields[0], ViewFieldShorthandConstants.All, StringComparison.InvariantCultureIgnoreCase))
        return string.Empty;
      viewFields = ResolveQueryFields(viewFields, list, trace);
      string viewFieldsXml = Caml.CAML.ViewFields(viewFields);
      return viewFieldsXml;
    }

    /// <summary>
    /// Converts a loosely typed hash table into a strongly
    /// typed dictionary. Each key is a field and each value
    /// is should parse to Ascending or Descending / Asc or Desc.
    /// </summary>
    /// <param name="orderByHt"></param>
    /// <returns></returns>
    public static Dictionary<string, CAML.SortType> ConvertToOrderBy(Hashtable orderByHt) {
      Dictionary<string, CAML.SortType> orderBy = null;
      if (orderByHt != null) {
        orderBy = new Dictionary<string, CAML.SortType>();
        foreach (string orderField in orderByHt.Keys) {
          string orderVal = orderByHt[orderField].ToString();
          CAML.SortType sort;
          if (!Enum.TryParse<CAML.SortType>(orderVal, out sort)) {
            if (string.Equals(orderVal, "desc", StringComparison.InvariantCultureIgnoreCase))
              sort = CAML.SortType.Descending;
            else // includes "asc"
              sort = CAML.SortType.Ascending;
          }
          orderBy.Add(orderField, sort);
        }
      }
      return orderBy;
    }

  }

}
