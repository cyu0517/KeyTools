using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;

namespace KeyWriter
{
    public class SuperDogDb
    {
        private readonly string _dataSource;

        private SQLiteConnection _connection;
        private SQLiteConnectionStringBuilder _connectionString;
        private SQLiteCommand _command;

        public SuperDogDb(string dataSource)
        {
            _dataSource = dataSource;
        }

        private bool Open()
        {
            try
            {
                _connection = new SQLiteConnection();
                _connectionString = new SQLiteConnectionStringBuilder { DataSource = _dataSource };
                _connection.ConnectionString = _connectionString.ConnectionString;
                _command = new SQLiteCommand { Connection = _connection };

                _connection.Open();
            }
            catch
            {
                Close();
                return false;
            }

            return true;
        }

        private void Close()
        {
            try
            {
                if (_connection != null)
                {
                    _connection.Close();
                    _connection.Dispose();
                    _connection = null;

                    if (_command != null)
                    {
                        _command.Parameters.Clear();
                        _command.Dispose();
                        _command = null;
                    }

                    _connectionString.Clear();
                    _connectionString = null;
                }
            }
            catch
            {
            }
        }

        private SQLiteDataReader ExecuteReader(string sqlCommand, SQLiteParameter param = null)
        {
            SQLiteDataReader reader = null;

            try
            {
                if (!string.IsNullOrEmpty(sqlCommand))
                {
                    _command.CommandText = sqlCommand;

                    if (param != null)
                    {
                        _command.Parameters.Add(param);
                    }

                    reader = _command.ExecuteReader();
                }
            }
            catch
            {
                return null;
            }

            return reader;
        }

        private int ExecuteNonQurey(string sqlCommand, SQLiteParameter[] parameters = null)
        {
            if (!Open())
            {
                return -1;
            }

            var result = -1;

            try
            {
                if (!string.IsNullOrEmpty(sqlCommand))
                {
                    _command.CommandText = sqlCommand;

                    if (parameters != null)
                    {
                        _command.Parameters.AddRange(parameters);
                    }

                    result = _command.ExecuteNonQuery();
                }
            }
            catch
            {
                return -1;
            }
            finally
            {
                Close();
            }

            return result;
        }

        private List<SuperDog> QuerySuperDog(string sql)
        {
            if (!Open())
            {
                return null;
            }

            var superDogList = new List<SuperDog>();

            try
            {
                var reader = ExecuteReader(sql);

                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        var superDog = new SuperDog
                            {
                                Id = !reader.IsDBNull(0) ? reader.GetInt32(0) : 0,
                                SuperDogGuid = !reader.IsDBNull(1) ? reader.GetString(1) : "",
                                SuperDogType = !reader.IsDBNull(2) ? reader.GetInt32(2) : 0,
                                MachineCode = !reader.IsDBNull(3) ? reader.GetString(3) : "",
                                CreateDate = !reader.IsDBNull(4) ? reader.GetString(4) : "",
                                UpdateDate = !reader.IsDBNull(5) ? reader.GetString(5) : "",
                                ExpireDate = !reader.IsDBNull(6) ? reader.GetString(6) : "",
                                ManufacturerId = !reader.IsDBNull(7) ? reader.GetInt32(7) : 0,
                                User = !reader.IsDBNull(8) ? reader.GetString(8) : "",
                                Remark = !reader.IsDBNull(9) ? reader.GetString(9) : ""
                            };

                        superDogList.Add(superDog);
                    }
                }

                return superDogList;
            }
            catch
            {
                return null;
            }
            finally
            {
                Close();
            }
        }

        private List<Manufacturer> QueryManufacturer(string sql)
        {
            if (!Open())
            {
                return null;
            }

            var manufacturerList = new List<Manufacturer>();

            try
            {
                var reader = ExecuteReader(sql);

                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        var manufacturer = new Manufacturer
                        {
                            Id = !reader.IsDBNull(0) ? reader.GetInt32(0) : 0,
                            Name = !reader.IsDBNull(1) ? reader.GetString(1) : ""
                        };

                        manufacturerList.Add(manufacturer);
                    }
                }

                return manufacturerList;
            }
            catch
            {
                return null;
            }
            finally
            {
                Close();
            }
        }

        public SuperDog GetSuperDog(string superDogGuid)
        {
            var sql = string.Format("SELECT * FROM SuperDog WHERE SuperDogGuid = '{0}'", superDogGuid);
            var superDogList = QuerySuperDog(sql);

            return superDogList != null && superDogList.Count > 0 ? superDogList[0] : null;
        }

        public SuperDog AddSuperDog(SuperDog superDog)
        {
            try
            {
                var parameters = new SQLiteParameter[3];
                const string sql = @"INSERT INTO SuperDog(SuperDogGuid,CreateDate,UpdateDate) VALUES(@SuperDogGuid,@CreateDate,@UpdateDate)";

                var parameter = new SQLiteParameter
                {
                    DbType = DbType.String,
                    ParameterName = "@SuperDogGuid",
                    Value = superDog.SuperDogGuid
                };
                parameters[0] = parameter;

                parameter = new SQLiteParameter
                {
                    DbType = DbType.String,
                    ParameterName = "@CreateDate",
                    Value = superDog.CreateDate
                };
                parameters[1] = parameter;

                parameter = new SQLiteParameter
                {
                    DbType = DbType.String,
                    ParameterName = "@UpdateDate",
                    Value = superDog.UpdateDate
                };
                parameters[2] = parameter;

                if (ExecuteNonQurey(sql, parameters) > 0)
                {
                    var superDogList = QuerySuperDog("SELECT * FROM SuperDog WHERE Id = (SELECT MAX(id) FROM SuperDog)");
                    return superDogList != null && superDogList.Count > 0 ? superDogList[0] : null;
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        public SuperDog ModifySuperDog(SuperDog superDog)
        {
            try
            {
                var parameters = new SQLiteParameter[8];
                const string sql = @"UPDATE SuperDog SET SuperDogType = @SuperDogType,
                                        MachineCode = @MachineCode,
                                        UpdateDate = @UpdateDate,
                                        ExpireDate = @ExpireDate,
                                        ManufacturerId = @ManufacturerId,
                                        User = @User,
                                        Remark = @Remark 
                                        WHERE SuperDogGuid = @SuperDogGuid";

                var parameter = new SQLiteParameter
                {
                    DbType = DbType.Int32,
                    ParameterName = "@SuperDogType",
                    Value = superDog.SuperDogType
                };
                parameters[0] = parameter;

                parameter = new SQLiteParameter
                {
                    DbType = DbType.String,
                    ParameterName = "@MachineCode",
                    Value = superDog.MachineCode
                };
                parameters[1] = parameter;

                parameter = new SQLiteParameter
                {
                    DbType = DbType.String,
                    ParameterName = "@UpdateDate",
                    Value = superDog.UpdateDate
                };
                parameters[2] = parameter;

                parameter = new SQLiteParameter
                {
                    DbType = DbType.String,
                    ParameterName = "@ExpireDate",
                    Value = superDog.ExpireDate
                };
                parameters[3] = parameter;

                parameter = new SQLiteParameter
                {
                    DbType = DbType.Int32,
                    ParameterName = "@ManufacturerId",
                    Value = superDog.ManufacturerId
                };
                parameters[4] = parameter;

                parameter = new SQLiteParameter
                {
                    DbType = DbType.String,
                    ParameterName = "@User",
                    Value = superDog.User
                };
                parameters[5] = parameter;

                parameter = new SQLiteParameter
                {
                    DbType = DbType.String,
                    ParameterName = "@Remark",
                    Value = superDog.Remark
                };
                parameters[6] = parameter;

                parameter = new SQLiteParameter
                {
                    DbType = DbType.String,
                    ParameterName = "@SuperDogGuid",
                    Value = superDog.SuperDogGuid
                };
                parameters[7] = parameter;

                if (ExecuteNonQurey(sql, parameters) > 0)
                {
                    return GetSuperDog(superDog.SuperDogGuid);
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        public bool RemoveSuperDog(string superDogGuid)
        {
            try
            {
                var sql = string.Format("DELETE FROM SuperDog WHERE SuperDogGuid = '{0}'", superDogGuid);
                var result = ExecuteNonQurey(sql);

                return result < 0;
            }
            catch
            {
                return false;
            }
        }

        public List<Manufacturer> GetManufacturers()
        {
            return QueryManufacturer("SELECT * FROM Manufacturer");
        }

        public Manufacturer AddManufacturer(Manufacturer manufacturer)
        {
            try
            {
                var parameters = new SQLiteParameter[1];
                const string sql = @"INSERT INTO Manufacturer(Name) VALUES(@Name)";

                var parameter = new SQLiteParameter
                {
                    DbType = DbType.String,
                    ParameterName = "@Name",
                    Value = manufacturer.Name
                };
                parameters[0] = parameter;

                if (ExecuteNonQurey(sql, parameters) > 0)
                {
                    var manufacturerList = QueryManufacturer("SELECT * FROM Manufacturer WHERE Id = (SELECT MAX(id) FROM Manufacturer)");
                    return manufacturerList != null && manufacturerList.Count > 0 ? manufacturerList[0] : null;
                }

                return null;
            }
            catch
            {
                return null;
            }
        }
    }

    public class SuperDog
    {
        public int Id { get; set; }
        public string SuperDogGuid { get; set; }
        public int SuperDogType { get; set; }
        public string MachineCode { get; set; }
        public string CreateDate { get; set; }
        public string UpdateDate { get; set; }
        public string ExpireDate { get; set; }
        public int ManufacturerId { get; set; }
        public string User { get; set; }
        public string Remark { get; set; }

        public SuperDog()
        {
            
        }

        public SuperDog(int id, string superDogGuid, int superDogType, string machineCode,
            string createDate, string updateDate, string expireDate, string user, string remark)
        {
            Id = id;
            SuperDogGuid = superDogGuid;
            SuperDogType = superDogType;
            MachineCode = machineCode;
            CreateDate = createDate;
            UpdateDate = updateDate;
            ExpireDate = expireDate;
            User = user;
            Remark = remark;
        }
    }

    public class Manufacturer
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public Manufacturer()
        {
        }

        public Manufacturer(int id, string name)
        {
            Id = id;
            Name = name;
        }
    }
}
