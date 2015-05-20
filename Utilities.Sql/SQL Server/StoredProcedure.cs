using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Utilities.Core;

namespace Utilities.Sql.SqlServer
{
  /*
    multiple result sets
    HasResultSet property
    Func<Boolean, T> booleanConverter
  
  ResultSets
    contains List<Columns> (hidden)
    index by [Int32]
    constructor takes a DataSet?

  */

  public class StoredProcedure : BaseSqlServerObject
  {
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

          DataSet dataset = null;
          var connection = this.Schema.Database.Server.Configuration.Connection;
          connection.ExecuteUnderDatabaseInvariant(this.Schema.Database.Name, () => dataset = connection.GetDataSet(this.SqlIdentifier, this.SqlParameters));
          foreach (DataTable table in dataset.Tables)
            this._resultSets.Add(new Columns(this, table));
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
    }

    public StoredProcedure(Schema schema, String name, Int32 versionNumber)
      : this(schema, name, versionNumber, new SqlParameter[] { })
    {
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
