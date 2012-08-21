using System;
using Mono.Data.Sqlite;
using System.Data;

namespace chat
{
	public class UserDB
	{
		public class DataStructure
		{
			public int id;
			public string username;
			public string password;
			public bool admin;
		}


		IDbConnection connection;
		IDbCommand command;

		[ThreadStatic]
		private static UserDB singelton;

		private UserDB ()
		{
			this.connection = (IDbConnection) new SqliteConnection("Data Source=chat.db");
			this.connection.Open();
		}

		public static UserDB GetInstance ()
		{
			if (singelton == null)
			{
				singelton = new UserDB();
				singelton.CreateTables();
			}

			return singelton;
		}

		private void CreateTables ()
		{
			lock (singelton) 
			{
				string sql = "Create Table IF NOT EXISTS users (id integer primary key autoincrement, username string unique, password string, admin bool)";
				this.command = (IDbCommand)this.connection.CreateCommand ();
				this.command.CommandText = sql;
				this.command.CommandType = CommandType.Text;
				this.command.ExecuteNonQuery ();
			}
		}

		public DataStructure FindByName (string username)
		{
			lock (singelton) 
			{
				try
				{
					string sql = "select * from users where username=:USERNAME limit 1";
					this.command = (IDbCommand)this.connection.CreateCommand ();
					this.command.CommandText = sql;

					SqliteParameter param = new SqliteParameter ();
					param.ParameterName = ":USERNAME";
					param.Value = username;
					param.DbType = DbType.String;

					this.command.Parameters.Add (param);
					IDataReader dr = (IDataReader)this.command.ExecuteReader ();

					if (dr.Read ()) 
					{
						return new DataStructure () { id = int.Parse((string)dr["id"].ToString()), username = dr["username"].ToString(), password = (string)dr["password"].ToString(), admin = bool.Parse(dr["admin"].ToString()) };
					}
					return null;
				}
				catch(SqliteException e)
				{
					return null;
				}
			}
		}

		public bool StoreUser (DataStructure user)
		{
			lock (singelton) 
			{
				try
				{
					string sql = "insert into users (username,password,admin) values (:USERNAME,:PASSWORD,:ADMIN)";
					SqliteParameter param1 = new SqliteParameter () { ParameterName = ":USERNAME", Value = user.username, DbType = DbType.String };
					SqliteParameter param2 = new SqliteParameter () { ParameterName = ":PASSWORD", Value = user.password, DbType = DbType.String };
					SqliteParameter param3 = new SqliteParameter () { ParameterName = ":ADMIN", Value = false, DbType = DbType.Boolean };

					this.command = (IDbCommand)this.connection.CreateCommand ();
					this.command.CommandText = sql;
					this.command.CommandType = CommandType.Text;

					this.command.Parameters.Add (param1);
					this.command.Parameters.Add (param2);
					this.command.Parameters.Add (param3);

					return (bool)(this.command.ExecuteNonQuery() == 0);
				}
				catch(SqliteException e)
				{
					return false;
				}

			}
		}

		public bool SetAdmin (int userId)
		{
			lock (singelton) 
			{
				try
				{
					string sql = "update users set admin=:ADMIN where id=:USERID";

					this.command = (IDbCommand)this.connection.CreateCommand ();
					this.command.CommandText = sql;
					this.command.CommandType = CommandType.Text;

					SqliteParameter param1 = new SqliteParameter () { ParameterName = ":ADMIN", Value = true, DbType = DbType.Boolean };
					SqliteParameter param2 = new SqliteParameter () { ParameterName = ":USERID", Value = userId, DbType = DbType.Int16 };

					this.command.Parameters.Add (param1);
					this.command.Parameters.Add (param2);
				
					return (bool)(this.command.ExecuteNonQuery () == 0);
				}
				catch(SqliteException e)
				{
					return false;
				}
			}
		}
	}
}

