﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Data.SqlClient;
using System.Windows.Forms;
using System.Data;

namespace ScriptQL
{
    [Serializable]
    public class SqlTable
    {
        readonly SqlSchema _parent;
        public string Name { get; private set; }
        public List<Column> ListColumn = new List<Column>();

        public SqlTable(SqlSchema parent, string name)
        {
            _parent = parent;
            Name = name;
        }

        public DataTable SelectFromTable(int rowsNumber, string column, string sortorder, int commandTimeout)
        {
            var conn = _parent.parent.Parent.GetConnection();
            var sb = new StringBuilder();
            sb.Append("SET ROWCOUNT @rowcount").Replace("@rowcount", rowsNumber.ToString(CultureInfo.InvariantCulture));
            sb.Append("USE [@dbname] ").Replace("@dbname", _parent.parent.Name);
            sb.Append("SELECT * FROM [@schema].[@table] with (NOLOCK)").Replace("@schema", _parent.name).Replace("@table", Name);
            sb.Append("ORDER BY [@column] @sortorder").Replace("@column", column).Replace("@sortorder", sortorder);

            var da = new SqlDataAdapter(sb.ToString(), conn);
            da.SelectCommand.CommandTimeout = commandTimeout;

            var dt = new DataTable(Name);

            try
            {
                conn.Open();
                da.Fill(dt);
                return dt;
            }
            catch (SqlException ex)
            {
                MessageBox.Show(ex.Message);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                conn.Close();
            }
            return null;


        }

        void AddColumnToTable(Column column)
        {
            ListColumn.Add(column);
        }


        public void BindData()
        {
            GetColumns();
        }

        private void GetColumns() // unificate with getViews
        {
            var sb = new StringBuilder();
            sb.Append("USE [@dbname] ");
            sb.Append("SELECT COLUMN_NAME FROM [@dbname].INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = '@schema' ");
            sb.Append("AND DATA_TYPE NOT IN('TEXT', 'NTEXT', 'IMAGE') AND TABLE_NAME = '@table' "); // not sortable data types
            sb.Append("ORDER BY COLUMN_NAME");

            sb.Replace("@dbname", _parent.parent.Name);
            sb.Replace("@schema", _parent.name);
            sb.Replace("@table", Name);

            SqlConnection conn = _parent.parent.Parent.GetConnection();
            var cmd = new SqlCommand(sb.ToString(), conn);

            try
            {
                conn.Open();
                Utils.WriteLog("executing : " + cmd.CommandText);
                SqlDataReader rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    var s = rdr.GetString((0));
                    var column = new Column(this, s);
                    AddColumnToTable(column);
                } 
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                conn.Close();
            }
        }
        public class SqlView : SqlTable
        {
            public SqlView(SqlSchema parent, string name) : base(parent, name)
            {

            }
        }
        [Serializable]
        public class Column
        {
            SqlTable _parent;
            public string name { get; set; }
            public Column(SqlTable parent, string name)
            {
                this._parent = parent;
                this.name = name;
            }

        }
    }
}
