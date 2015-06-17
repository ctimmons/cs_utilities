using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

using Utilities.Core;

namespace Utilities.Sql.SqlServer
{
  public class StoredProcedure : BaseSqlServerObject
  {
    private Configuration _configuration;

    public Schema Schema { get; private set; }
    public Int32 VersionNumber { get; private set; }
    public SqlParameter[] SqlParameters { get; private set; }
    public Boolean DoesReturnResultSet { get; private set; }

    private List<Columns> _resultSets = null;
    public List<Columns> ResultSets
    {
      get
      {
        if (this._resultSets == null)
        {
          this._resultSets = new List<Columns>();
          var connection = this._configuration.Connection;

          connection.ExecuteUnderDatabaseInvariant(this.Schema.Database.Name,
            () =>
            {
              var dataset = connection.GetDataSet(this.SqlIdentifier, this.SqlParameters);
              foreach (DataTable table in dataset.Tables)
                this._resultSets.Add(new Columns(this, table));
            });
        }

        return this._resultSets;
      }
    }

    public String SqlIdentifier
    {
      get
      {
        return String.Concat(this.Schema.BracketedName, ".", this.BracketedName);
      }
    }

    public override String TargetLanguageIdentifier
    {
      get
      {
        return String.Concat(this.Schema.Name, "_", this.Name, "_", this.VersionNumber.ToString());
      }
    }

    private StoredProcedure()
      : base()
    {
    }

    public StoredProcedure(Schema schema, String name, Int32 versionNumber, SqlParameter[] sqlParameters)
      : this()
    {
      this.Schema = schema;
      this.Name = IdentifierHelper.GetStrippedSqlIdentifier(name);
      this.VersionNumber = versionNumber;
      this.SqlParameters = sqlParameters;
      this.DoesReturnResultSet = true;

      this._configuration = this.Schema.Database.Server.Configuration;
    }

    public StoredProcedure(Schema schema, String name, Int32 versionNumber)
      : this(schema, name, versionNumber, new SqlParameter[] { })
    {
    }

    public IEnumerable<String> GetTargetLanguageMethodParameterNameAndTypes()
    {
      return this.SqlParameters.Select(p => this.GetTargetLanguageMethodParameterNameAndType(p));
    }

    /// <summary>
    /// Return a string that can be used in generated code to represent this column as a target language method parameter.
    /// </summary>
    private String GetTargetLanguageMethodParameterNameAndType(SqlParameter sqlParameter)
    {
      var format = "";

      if (this._configuration.TargetLanguage.IsCSharp())
        format = "{0} {1}";
      else if (this._configuration.TargetLanguage.IsFSharp())
        format = "({1} : {0})";
      else if (this._configuration.TargetLanguage.IsVisualBasic())
        format = "{1} As {0}";
      else
        throw new NotImplementedException(String.Format(Properties.Resources.UnknownTargetLanguageValue, this._configuration.TargetLanguage));

      return String.Format(format, this.GetClrTypeNameFromSqlDbType(sqlParameter), IdentifierHelper.GetTargetLanguageIdentifier(sqlParameter.ParameterName)).Trim();
    }

    private String GetClrTypeNameFromSqlDbType(SqlParameter sqlParameter)
    {
      Func<String, String> getAppropriateClrType =
        clrType =>
        {
          if (this._configuration.TargetLanguage.IsCSharp() || this._configuration.TargetLanguage.IsFSharp())
            return sqlParameter.IsNullable ? "System.Nullable<System." + clrType + ">" : "System." + clrType;
          else if (this._configuration.TargetLanguage.IsVisualBasic())
            return sqlParameter.IsNullable ? "System.Nullable(Of System." + clrType + ")" : "System." + clrType;
          else
            throw new NotImplementedException(String.Format(Properties.Resources.UnknownTargetLanguageValue, this._configuration.TargetLanguage));
        };

      switch (sqlParameter.SqlDbType)
      {
        case SqlDbType.BigInt:
          return getAppropriateClrType("Int64");
        case SqlDbType.Binary:
        case SqlDbType.VarBinary:
        case SqlDbType.Image:
        case SqlDbType.Timestamp:
          if (this._configuration.TargetLanguage.IsCSharp() || this._configuration.TargetLanguage.IsFSharp())
            return "System.Byte[]";
          else if (this._configuration.TargetLanguage.IsVisualBasic())
            return "System.Byte()";
          else
            throw new NotImplementedException(String.Format(Properties.Resources.UnknownTargetLanguageValue, this._configuration.TargetLanguage));
        case SqlDbType.Bit:
          return getAppropriateClrType("Boolean");
        case SqlDbType.Date:
        case SqlDbType.DateTime:
        case SqlDbType.DateTime2:
        case SqlDbType.SmallDateTime:
          return getAppropriateClrType("DateTime");
        case SqlDbType.DateTimeOffset:
          return getAppropriateClrType("DateTimeOffset");
        case SqlDbType.Decimal:
        case SqlDbType.Money:
        case SqlDbType.SmallMoney:
          return getAppropriateClrType("Decimal");
        case SqlDbType.Float:
          return getAppropriateClrType("Double");
        case SqlDbType.Structured:
          return "System.Data.DataTable";
        case SqlDbType.Int:
          return getAppropriateClrType("Int32");
        case SqlDbType.Char:
        case SqlDbType.NChar:
        case SqlDbType.NText:
        case SqlDbType.NVarChar:
        case SqlDbType.Text:
        case SqlDbType.VarChar:
          return "System.String";
        case SqlDbType.Xml:
          switch (this._configuration.XmlSystem)
          {
            case XmlSystem.AsString:
              return "System.String";
            case XmlSystem.Linq_XDocument:
              return "System.Xml.Linq.XElement";
            case XmlSystem.NonLinq_XmlDocument:
              return "System.Xml.XmlDocument";
            default:
              return String.Format(Properties.Resources.UnknownXmlSystemValue, this._configuration.XmlSystem);
          }
        case SqlDbType.Real:
          return getAppropriateClrType("Single");
        case SqlDbType.SmallInt:
          return getAppropriateClrType("Int16");
        case SqlDbType.Variant:
          return "System.Object";
        case SqlDbType.Time:
          return getAppropriateClrType("TimeSpan");
        case SqlDbType.TinyInt:
          return getAppropriateClrType("Byte");
        case SqlDbType.UniqueIdentifier:
          return getAppropriateClrType("Guid");
        default:
          return String.Format("ERROR - Can't find CLR type that corresponds to SqlDbType.{0}.", sqlParameter.SqlDbType);
      }
    }
  }

  public static class StoredProcedureExtensions
  {
    public static StoredProcedure GetByName(this IEnumerable<StoredProcedure> storedProcedures, String name)
    {
      return storedProcedures.GetByName(name, 1);
    }

    public static StoredProcedure GetByName(this IEnumerable<StoredProcedure> storedProcedures, String name, Int32 versionNumber)
    {
      return storedProcedures.Where(sp => sp.Name.EqualsCI(name) && (sp.VersionNumber == versionNumber)).FirstOrDefault();
    }
  }
}
