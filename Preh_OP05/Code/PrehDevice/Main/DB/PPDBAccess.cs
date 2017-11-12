using System;
using System.Linq;
using System.Data;
using System.Data.SqlClient;
using System.Collections.Generic;

/* -------------------------------------------------------------------------------------------------------- */
/* PPDBAccess - Preh Portugal                                                                               */
/* -------------------------------------------------------------------------------------------------------- */
/*                                                                                                          */
/* IMPORTANT WARNING !!!!!                                                                                  */
/*                                                                                                          */
/* Please do NOT ADD, CHANGE or REMOVE any methods from this file.                                          */
/* Create a new object and inherit from one of the available classes: PPBaseDB, PPTraceDB or PPTraceStation */
/*                                                                                                          */
/* -------------------------------------------------------------------------------------------------------- */

namespace PPDBAccess
{
    #region Generic Enums
    public enum ResultCategory
    {
        Acoustic = 9,
        Electric = 2,
        HapticFeedback = 12,
        KeysMechanical = 3,
        KeysSensor = 4,
        Mechanical = 7,
        Operator = 1,
        Software = 8,
        System = 11,
        Torque = 5,
        TouchSurfaces = 10,
        Vision = 6,
    }
    public enum ResultSubCategory
    {
        Acoustic_Actuator = 42,
        Acoustic_Noise = 30,
        Acoustic_Speaker = 29,
        Electric_Currents = 2,
        Electric_Signals = 5,
        Electric_States = 4,
        Electric_Voltages = 3,
        KeysMechanical_Blind = 9,
        KeysMechanical_Joystick = 8,
        KeysMechanical_PushButton = 6,
        KeysMechanical_Rocker = 7,
        KeysSensor_TouchKeys = 10,
        Mechanical_BezelCheck = 45,
        Mechanical_DimensionControl = 21,
        Mechanical_MountingBrackets = 20,
        Mechanical_PINControl = 19,
        Mechanical_ScrewCheck = 18,
        Operator_Evaluations = 1,
        Software_Calibrations = 25,
        Software_Checksum = 22,
        Software_Communications = 26,
        Software_DataIdentifiers = 24,
        Software_DTC = 27,
        Software_EEPROM = 23,
        Software_Label = 43,
        Software_Signals = 28,
        System_Calibration = 37,
        System_ConfigFiles = 33,
        System_Database = 36,
        System_Hardware = 35,
        System_Software = 34,
        Torque_RotaryButton = 11,
        TouchSurfaces_TouchDisplays = 31,
        TouchSurfaces_TouchPads = 32,
        Vision_Display = 16,
        Vision_KeySymbols = 12,
        Vision_LaserEtching = 15,
        Vision_LED = 17,
        Vision_Luminance = 13,
        Vision_Variants = 14,
    }
    public enum ResultUnit
    {
        mV = 1,
        V = 2,
        uA = 3,
        mA = 4,
        A = 5,
        Hz = 6,
        Percent = 7,
        Text = 8,
        Bool = 9,
        HEX = 10,
        N = 11,
        mm = 12,
        Ncm = 13,
        Count = 14,
        Px = 15,
        C = 16,
        ms = 17,
        Nmm = 18,
        cd_m2 = 19,
        raw = 20,
        dB = 21,
        um = 22,
    }
    public enum Language
    {
        PT = 1,
        EN = 2,
        DE = 3,
        RO = 4,
        ES = 5,
        CN = 6,
    }
    #endregion

    #region Exceptions
    public class SamplesLoadException : Exception
    {
        public SamplesLoadException()
          : base("Error loading Samples!")
        {
        }
        public SamplesLoadException(string message)
          : base(message)
        {
        }
        public SamplesLoadException(string message, Exception inner)
          : base(message, inner)
        {
        }
    }
    public class ResultCharacteristicsLoadException : Exception
    {
        public ResultCharacteristicsLoadException()
          : base("Error loading Result Characteristics!")
        {
        }
        public ResultCharacteristicsLoadException(string message)
          : base(message)
        {
        }
        public ResultCharacteristicsLoadException(string message, Exception inner)
          : base(message, inner)
        {
        }
    }
    public class ResultObjectsLoadException : Exception
    {
        public ResultObjectsLoadException()
          : base("Error loading Result Objects!")
        {
        }
        public ResultObjectsLoadException(string message)
          : base(message)
        {
        }
        public ResultObjectsLoadException(string message, Exception inner)
          : base(message, inner)
        {
        }
    }
    public class ResultSubCategoriesLoadException : Exception
    {
        public ResultSubCategoriesLoadException()
          : base("Error loading Result Categories/SubCategories!")
        {
        }
        public ResultSubCategoriesLoadException(string message)
          : base(message)
        {
        }
        public ResultSubCategoriesLoadException(string message, Exception inner)
          : base(message, inner)
        {
        }
    }
    public class ResultSubCategoriesValidationException : Exception
    {
        public ResultSubCategoriesValidationException()
          : base("The Categories/SubCategories in Enum does not match the list in Database!")
        {
        }
        public ResultSubCategoriesValidationException(string message)
          : base(message)
        {
        }
        public ResultSubCategoriesValidationException(string message, Exception inner)
          : base(message, inner)
        {
        }
    }
    public class MeasureLimitNotFoundException : Exception
    {
        public MeasureLimitNotFoundException()
          : base("The specified MeasureLimit name was not found!")
        {
        }
        public MeasureLimitNotFoundException(string message)
          : base(message)
        {
        }
        public MeasureLimitNotFoundException(string message, Exception inner)
          : base(message, inner)
        {
        }
    }
    public class ConstantNotFoundException : Exception
    {
        public ConstantNotFoundException()
          : base("The specified Constant was not found!")
        {
        }
        public ConstantNotFoundException(string message)
          : base(message)
        {
        }
        public ConstantNotFoundException(string message, Exception inner)
          : base(message, inner)
        {
        }
    }
    public class ConfigurationNotFoundException : Exception
    {
        public ConfigurationNotFoundException()
          : base("The specified Configuration name was not found!")
        {
        }
        public ConfigurationNotFoundException(string message)
          : base(message)
        {
        }
        public ConfigurationNotFoundException(string message, Exception inner)
          : base(message, inner)
        {
        }
    }
    public class StationNotFoundException : Exception
    {
        public StationNotFoundException()
          : this("The specified Station was not found! Please check IDWS and SubWS values!")
        {
        }
        public StationNotFoundException(string message)
          : base(message)
        {
        }
        public StationNotFoundException(string message, Exception inner)
          : base(message, inner)
        {
        }
    }
    public class SQLApplicationNameException : Exception
    {
        public SQLApplicationNameException()
          : this("The ApplicationName field must not be empty!")
        {
        }
        public SQLApplicationNameException(string message)
          : base(message)
        {
        }
        public SQLApplicationNameException(string message, Exception inner)
          : base(message, inner)
        {
        }
    }
    public class SQLConnectionTimeoutException : Exception
    {
        public SQLConnectionTimeoutException()
          : this("The SQL connection timeout limit must be grater then 0 and lower than 30 seconds!")
        {
        }
        public SQLConnectionTimeoutException(string message)
          : base(message)
        {
        }
        public SQLConnectionTimeoutException(string message, Exception inner)
          : base(message, inner)
        {
        }
    }
    #endregion

    #region Generic Objects
    public class Reference
    {
        public int IDRef { get; set; }
        public int IDRefProfile { get; set; }
        public int IDRefSeqGroup { get; set; }
        public string RefPreh { get; set; }
        public bool Active { get; set; }
        public string RefDescription { get; set; }
        public string RefShortType { get; set; }
        public string ExtraCode { get; set; }
    }
    public class AppUser
    {
        public int ID_User { get; set; }
        public string Identification { get; set; }
        public string UserName { get; set; }
        public string Department { get; set; }
        public int AccessMask { get; set; }
        public string Psw { get; set; }
    }
    public class TraceErrorMessage
    {
        public int TraceErrorCode { get; set; }
        public string TraceErrorDescription { get; set; } //= "Error description unavailable";
    }
    public class Message
    {
        public int ID_Msg { get; set; }
        public int ID_SubWS { get; set; }
        public string Portuguese { get; set; }
        public string English { get; set; }
        public string German { get; set; }
        public string Romanian { get; set; }
        public string Spanish { get; set; }
        public string Chinese { get; set; }
        public string DefaultLanguage { get; set; }
    }
    public class DeviceConst
    {
        public int ID_Const { get; set; }
        public int ID_SubWS { get; set; }
        public string ConstName { get; set; }
        public string ConstValue { get; set; }
        public string ConstDescription { get; set; }
    }
    public class Sample
    {
        public int ID_Sample { get; set; }
        public long TraceNr { get; set; }
        public int ID_SubWS { get; set; }
        public int IDWS { get; set; }
        public int SubWS { get; set; }
        public string ErrorCode { get; set; }
        public string SampleDetails { get; set; }
        public bool OkSample { get; set; }
        public bool IsSampleForThisWS { get; set; }
    }
    public class TestErrorResult
    {
        public int ID_WSJob { get; set; }
        public int ID_DeviceResultCategory { get; set; }
        public int ID_DeviceResultSubCategory { get; set; }
        public int ID_DeviceResultTestCharacteristic { get; set; }
        public int ID_DeviceResultTestObject { get; set; }
        public string SubCategoryName { get; set; }
        public string CharacteristicName { get; set; }
        public string TestObjectName { get; set; }
        public string ErrorCode { get { return ID_DeviceResultCategory + "." + ID_DeviceResultSubCategory + "." + ID_DeviceResultTestCharacteristic + "." + ID_DeviceResultTestObject; } }
        public string ErrorDescripton { get { return SubCategoryName + "." + CharacteristicName + "." + TestObjectName; } }
    }
    public class Caliber
    {
        public long TraceNr { get; set; }
        public int ID_DefaultValue { get; set; }
        public int IDWS { get; set; }
        public int SubWS { get; set; }
        public int ID_SubWS { get; set; }
    }
    public class ResultSubCategoryInDB
    {
        public int ID_DeviceResultSubCategory { get; set; }
        public int ID_DeviceResultCategory { get; set; }
        public string SubCategoryDescription { get; set; }
    }
    public class ResultCharacteristic
    {
        public int ID_DeviceResultTestCharacteristic { get; set; }
        public int ID_DeviceResultSubCategory { get; set; }
        public int ID_DeviceResultCategory { get; set; }
        public string CharacteristicName { get; set; }
    }
    public class ResultObject
    {
        public int ID_DeviceResultTestObject { get; set; }
        public string TestObjectName { get; set; }
    }
    public class TraceNrHistoryItem
    {
        public string JobID { get; set; }
        public DateTime? StartTime { get; set; }
        public string Action { get; set; }
        public string JobResult { get; set; }
        public string ErrorCode { get; set; }
        public string ErrorDetails { get; set; }
        public int Rank1 { get; set; }
        public int Rank2 { get; set; }
    }
    public class EBAUExportDefinition
    {
        public int ID_EbauExportDef { get; set; }
        public int ID_WSCenter { get; set; }
        public string RefPrehShort { get; set; }
        public int DestinationSite { get; set; }
        public bool ExportActive { get; set; }
    }
    public class LaserTask
    {
        public string LaserProfile { get; set; }
        public string JOBName { get; set; }
        public string BINName { get; set; }
        public string BINDescription { get; set; }
        public bool CompleteJob { get; set; }
        public bool FromRework { get; set; }
    }
    public class MeasureLimit
    {
        public int ID_MeasureLimit { get; set; }
        public int ID_Ref { get; set; }
        public string MeasureProfile { get; set; }
        public int ID_SubWS { get; set; }
        public int ID_WS { get; set; }
        public int SubWS { get; set; }
        public string MeasureName { get; set; }
        public float MinValue { get; set; }
        public float MaxValue { get; set; }
        public string Unit { get; set; }
        public bool Active { get; set; }
        public string Description { get; set; }
        public string Variant { get; set; }
        public string ProfileType { get; set; }
    }
    public class SystemConfiguration
    {
        public int ID_SystemConfiguration { get; set; }
        public int ID_Ref { get; set; }
        public string ConfigurationProfile { get; set; }
        public int ID_SubWS { get; set; }
        public int ID_WS { get; set; }
        public int SubWS { get; set; }
        public string ConfigurationName { get; set; }
        public string Value { get; set; }
        public string Unit { get; set; }
        public bool Active { get; set; }
        public string Description { get; set; }
        public string Variant { get; set; }
        public string ProfileType { get; set; }
    }
    public class PartErrorConfiguration
    {
        public int ID_ErrorConfiguration { get; set; }
        public int ID_DeviceResultTestCharacteristic { get; set; }
        public int ID_DeviceResultTestObject { get; set; }
        public int ID_WSJob { get; set; }
        public string ErrorCode { get; set; }
        public string Causes { get; set; }
        public string Actions { get; set; }
        public bool TreatAsCritical { get; set; }
    }
    public class CalibrationMeasurement
    {
        public int ID_DefaultValue { get; set; }
        public string Name { get; set; }
        public double NominalValue { get; set; }
        public double Tolerance { get; set; }
        public string Units { get; set; }
        public double CalibrationError { get; set; }
    }
    public class CalibrationAdjustment
    {
        public DateTime CalibrationDate { get; set; }
        public int ElapsedDays { get { return (int)(DateTime.Now - CalibrationDate).TotalDays; } }
        public string Name { get; set; }
        public double Value { get; set; }
        public double OffSet { get; set; }
    }
    #endregion


    /// <summary>
    /// Base methods for connecting and executing SQL server commands.
    /// </summary>
    public class PPBaseDB
    {
        #region Vars
        protected SqlConnectionStringBuilder sb_DefaultDBConnectionString;
        #endregion

        #region Properties
        public string Version { get { return "2.0.5.0"; } }
        public string LastErrorDescription { get; set; }
        public string DefaultDBSetConnectionString
        {
            get { return sb_DefaultDBConnectionString.ConnectionString; }
            set
            {
                sb_DefaultDBConnectionString = new SqlConnectionStringBuilder(value);
                if (sb_DefaultDBConnectionString.ApplicationName.Trim().Equals("")) { throw new SQLApplicationNameException("The ApplicationName field must not be empty!"); }
            }
        }
        public string DefaultDBServerName
        {
            get { return sb_DefaultDBConnectionString.DataSource; }
        }
        public string DefaultDBDatabaseName
        {
            get { return sb_DefaultDBConnectionString.InitialCatalog; }
        }
        #endregion

        #region Constructors
        /// <summary>
        ///
        /// </summary>
        /// <param name="DefaultDBConnectionString">The default connection string for this instance</param>
        public PPBaseDB(SqlConnectionStringBuilder DefaultDBConnectionString)
          : this(DefaultDBConnectionString, 5)
        {
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="DefaultDBConnectionString">The default connection string for this instance</param>
        /// <param name="ConnectionTimeout">The SQL connection timeout limit [0 - 30]</param>
        public PPBaseDB(SqlConnectionStringBuilder DefaultDBConnectionString, int ConnectionTimeout)
        {
            if (ConnectionTimeout <= 0 || ConnectionTimeout > 30) throw new SQLConnectionTimeoutException();
            try
            {
                this.sb_DefaultDBConnectionString = new SqlConnectionStringBuilder(DefaultDBConnectionString.ConnectionString);
                this.sb_DefaultDBConnectionString.ConnectTimeout = ConnectionTimeout;
            }
            catch (Exception) { this.sb_DefaultDBConnectionString = null; }
        }
        #endregion

        #region Methods
        public bool DBExecuteNonQuery(string query, string connectionstring, int CommandTimeout)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionstring))
                {
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        connection.Open();
                        command.CommandTimeout = CommandTimeout;
                        command.ExecuteNonQuery();
                        connection.Close();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Error in DBExecute. Query =  " + query + "; Error = " + ex.ToString();
                return false;
            }
        }
        public bool DBExecuteNonQuery(string query, string connectionstring)
        {
            return DBExecuteNonQuery(query, sb_DefaultDBConnectionString.ConnectionString, 30);
        }
        public bool DBExecuteNonQuery(string query)
        {
            return DBExecuteNonQuery(query, sb_DefaultDBConnectionString.ConnectionString);
        }
        public bool DBExecuteNonQuery(ref SqlCommand command, string connectionstring, int CommandTimeout)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionstring))
                {
                    connection.Open();
                    command.CommandTimeout = CommandTimeout;
                    command.Connection = connection;
                    command.ExecuteNonQuery();
                }
                return true;
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Error in DBExecute. Query =  " + command.CommandText + "; Error = " + ex.ToString();
                return false;
            }
        }
        public bool DBExecuteNonQuery(ref SqlCommand command, string connectionstring)
        {
            return DBExecuteNonQuery(ref command, sb_DefaultDBConnectionString.ConnectionString, 30);
        }
        public bool DBExecuteNonQuery(ref SqlCommand command)
        {
            return DBExecuteNonQuery(ref command, sb_DefaultDBConnectionString.ConnectionString);
        }

        public object DBExecuteScalar(SqlCommand command, string connectionstring, int CommandTimeout)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionstring))
                {
                    connection.Open();
                    command.CommandTimeout = CommandTimeout;
                    command.Connection = connection;
                    return command.ExecuteScalar();
                }
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Error in DBExecuteScalar. Query =  " + command.CommandText + "; Error = " + ex.ToString();
                return null;
            }
        }
        public object DBExecuteScalar(SqlCommand command, string connectionstring)
        {
            return DBExecuteScalar(command, sb_DefaultDBConnectionString.ConnectionString, 30);
        }
        public object DBExecuteScalar(SqlCommand command)
        {
            return DBExecuteScalar(command, sb_DefaultDBConnectionString.ConnectionString);
        }

        public DataSet FillDataSetBySQL(string query, string connectionstring, int CommandTimeout)
        {
            DataSet ds = new DataSet();
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionstring))
                {
                    using (SqlDataAdapter dataadapter = new SqlDataAdapter(query, connection))
                    {
                        connection.Open();
                        dataadapter.SelectCommand.CommandTimeout = CommandTimeout;
                        dataadapter.Fill(ds);
                        connection.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Error in FillDatasetBySQL. Query =  " + query + "; Error = " + ex.ToString();
                return null;
            }
            return ds;
        }
        public DataSet FillDataSetBySQL(string query, string connectionstring)
        {
            return this.FillDataSetBySQL(query, sb_DefaultDBConnectionString.ConnectionString, 30);
        }
        public DataSet FillDataSetBySQL(string query)
        {
            return this.FillDataSetBySQL(query, sb_DefaultDBConnectionString.ConnectionString);
        }

        public DataTable FillDataTableBySQL(string query, string connectionstring, int CommandTimeout)
        {
            DataTable dt = new DataTable();
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionstring))
                {
                    using (SqlDataAdapter dataadapter = new SqlDataAdapter(query, connection))
                    {
                        connection.Open();
                        dataadapter.SelectCommand.CommandTimeout = CommandTimeout;
                        dataadapter.Fill(dt);
                        connection.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Error in FillDataTableBySQL. Query =  " + query + "; Error = " + ex.ToString();
                return null;
            }
            return dt;
        }
        public DataTable FillDataTableBySQL(string query, string connectionstring)
        {
            return this.FillDataTableBySQL(query, connectionstring, 30);
        }
        public DataTable FillDataTableBySQL(string query)
        {
            return this.FillDataTableBySQL(query, sb_DefaultDBConnectionString.ConnectionString);
        }

        public IEnumerable<IDataRecord> FillDataReaderBySQL(string query, string connectionstring, int CommandTimeout)
        {
            using (SqlConnection connection = new SqlConnection(connectionstring))
            {
                using (SqlCommand command = new SqlCommand(query, connection))
                {

                    connection.Open();
                    command.CommandTimeout = CommandTimeout;

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            yield return reader;
                        }
                    }
                }
            }
        }
        public IEnumerable<IDataRecord> FillDataReaderBySQL(string query, string connectionstring)
        {
            return this.FillDataReaderBySQL(query, connectionstring, 30);
        }
        public IEnumerable<IDataRecord> FillDataReaderBySQL(string query)
        {
            return this.FillDataReaderBySQL(query, sb_DefaultDBConnectionString.ConnectionString);
        }

        public IEnumerable<IDataRecord> FillDataReaderBySQL(SqlCommand command, string connectionstring, int CommandTimeout)
        {
            using (SqlConnection connection = new SqlConnection(connectionstring))
            {
                connection.Open();
                command.CommandTimeout = CommandTimeout;
                command.Connection = connection;

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        yield return reader;
                    }
                }
            }
        }
        public IEnumerable<IDataRecord> FillDataReaderBySQL(SqlCommand command, string connectionstring)
        {
            return this.FillDataReaderBySQL(command, connectionstring, 30);
        }
        public IEnumerable<IDataRecord> FillDataReaderBySQL(SqlCommand command)
        {
            return this.FillDataReaderBySQL(command, sb_DefaultDBConnectionString.ConnectionString);
        }

        #endregion
    }


    /// <summary>
    /// All methods with no specific station. inherits PPBaseDB. 
    /// </summary>
    public class PPTraceDB : PPBaseDB
    {
        #region Vars
        protected SqlConnectionStringBuilder sb_ProdGenericsConnectionString;
        #endregion

        #region Properties
        public AppUser AuthenticatedUser { get; set; }
        public string ProdGenericsSetConnectionString
        {
            get { return sb_ProdGenericsConnectionString.ConnectionString; }
            set
            {
                sb_ProdGenericsConnectionString = new SqlConnectionStringBuilder(value);
                if (sb_ProdGenericsConnectionString.ApplicationName.Trim().Equals("")) { throw new SQLApplicationNameException(); }
            }
        }
        public string ProdGenericsServerName
        {
            get { return sb_ProdGenericsConnectionString.DataSource; }
        }
        public string ProdGenericsDatabaseName
        {
            get { return sb_DefaultDBConnectionString.InitialCatalog; }
        }
        public bool Language_AutoInsertNewMessages { get; set; }
        public Language SystemLanguage { get; set; }
        public List<DeviceConst> DeviceConsts { get; set; }
        public List<AppUser> AppUsers { get; set; }
        public List<TraceErrorMessage> TraceErrorMessages { get; set; }
        public List<Message> Messages { get; set; }
        public List<Sample> Samples { get; set; }
        public List<TestErrorResult> TestErrorResults { get; set; }
        public List<Caliber> Calibers { get; set; }
        public List<ResultCharacteristic> ResultCharacteristics { get; set; }
        public List<ResultObject> ResultObjects { get; set; }
        public List<ResultSubCategoryInDB> ResultSubCategories { get; set; }
        public List<MeasureLimit> MeasureLimits { get; set; }
        public List<SystemConfiguration> SystemConfigurations { get; set; }
        public List<Reference> References { get; set; }
        public int CaliberCount { get { return Calibers == null ? 0 : Calibers.Count(); } }
        public int CaliberDefaultValue { get; set; }
        public int SampleCount { get { return Samples == null ? 0 : Samples.Count(); } }
        //public string ErrorCode { get; set; }
        #endregion

        #region Constructors
        /// <summary>
        /// 
        /// </summary>
        /// <param name="LineDBConnectionString">The default connection string for this instance</param>
        /// <param name="LoadDataOnStartUp">Loads Devices results data on startup</param>
        public PPTraceDB(SqlConnectionStringBuilder LineDBConnectionString, bool LoadDataOnStartUp = true)
          : this(LineDBConnectionString, 5, LoadDataOnStartUp)
        {
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="LineDBConnectionString">The default connection string for this instance</param>
        /// <param name="ConnectionTimeout">The SQL connection timeout limit [0 - 30]</param>
        /// <param name="LoadDataOnStartUp">Loads Devices results data on startup</param>
        public PPTraceDB(SqlConnectionStringBuilder LineDBConnectionString, int ConnectionTimeout, bool LoadDataOnStartUp = true)
          : base(LineDBConnectionString, ConnectionTimeout)
        {
            if (LoadDataOnStartUp)
            {
                if (!Result_LoadResultCharacteristics()) throw new ResultCharacteristicsLoadException();
                if (!Result_LoadResultObjects()) throw new ResultObjectsLoadException();
                if (!Result_LoadResultSubCategories()) throw new ResultSubCategoriesLoadException();
                if (!ValidateResultSubCategories()) throw new ResultSubCategoriesValidationException();
                if (!Samples_LoadSamples()) throw new SamplesLoadException();
            }

            Language_AutoInsertNewMessages = true;

            SystemLanguage = Language.EN;
            if (sb_ProdGenericsConnectionString != null) Errors_LoadTraceErrorsLanguage();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="LineDBConnectionString">The default connection string for this instance</param>
        /// <param name="ProdGenericsConnectionString">The ProdGenerics/ProdGeral database connection string for this instance</param>
        /// <param name="LoadDataOnStartUp">Loads Devices results data on startup</param>
        public PPTraceDB(SqlConnectionStringBuilder LineDBConnectionString, SqlConnectionStringBuilder ProdGenericsConnectionString, bool LoadDataOnStartUp = true)
          : this(LineDBConnectionString, ProdGenericsConnectionString, 5, LoadDataOnStartUp)
        {
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="LineDBConnectionString">The default connection string for this instance</param>
        /// <param name="ProdGenericsConnectionString">The ProdGenerics/ProdGeral database connection string for this instance</param>
        /// <param name="ConnectionTimeout">The SQL connection timeout limit [0 - 30]</param>
        /// <param name="LoadDataOnStartUp">Loads Devices results data on startup</param>
        public PPTraceDB(SqlConnectionStringBuilder LineDBConnectionString, SqlConnectionStringBuilder ProdGenericsConnectionString, int ConnectionTimeout, bool LoadDataOnStartUp = true)
          : this(LineDBConnectionString, ConnectionTimeout, LoadDataOnStartUp)
        {
            this.sb_ProdGenericsConnectionString = new SqlConnectionStringBuilder(ProdGenericsConnectionString.ConnectionString);
            this.sb_ProdGenericsConnectionString.ConnectTimeout = ConnectionTimeout;

            Errors_LoadTraceErrorsLanguage();
        }

        #endregion

        #region Methods - EBAU
        public List<EBAUExportDefinition> GetAOIEbauExportSettings()
        {
            List<EBAUExportDefinition> EBAUExportDefinitions = new List<EBAUExportDefinition>();
            try
            {
                IEnumerable<IDataRecord> readerlist = FillDataReaderBySQL("EXEC EbauExport_GetExportDefinitions", sb_DefaultDBConnectionString.ConnectionString);
                foreach (var reader in readerlist)
                {
                    EBAUExportDefinitions.Add(new EBAUExportDefinition
                    {
                        ID_EbauExportDef = (int)reader["ID_EbauExportDef"],
                        ID_WSCenter = (int)reader["ID_WSCenter"],
                        DestinationSite = (int)reader["DestinationSite"],
                        RefPrehShort = (string)reader["RefPrehShort"],
                        ExportActive = (bool)reader["ExportActive"],
                    });
                }
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Error in GetAOIEbauExportSettings: " + ex.ToString();
                return null;
            }
            return EBAUExportDefinitions;
        }
        public bool EBAU_UpdateExportDefinition(int ID_EbauExportDef, string Reference, string DestinationSite, bool Active)
        {
            SqlCommand cmd = new SqlCommand();
            cmd.CommandText = "EbauExport_UpdateExportDefinition";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@ID_EbauExportDef", ID_EbauExportDef);
            cmd.Parameters["@ID_EbauExportDef"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@RefPrehShort", Reference);
            cmd.Parameters["@RefPrehShort"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@DestinationSite", DestinationSite);
            cmd.Parameters["@DestinationSite"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@ExportActive", Active);
            cmd.Parameters["@ExportActive"].Direction = ParameterDirection.Input;

            cmd.Parameters.Add("@Return", SqlDbType.Int);
            cmd.Parameters["@Return"].Direction = ParameterDirection.ReturnValue;

            try
            {
                return DBExecuteNonQuery(ref cmd) ? (int)cmd.Parameters["@Return"].Value == 0 : false;
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Error in EBAU_UpdateExportDefinition: " + ex.ToString();
                return false;
            }
        }
        public bool EBAU_InsertExportDefinition(string Reference, string DestinationSite, bool Active)
        {
            SqlCommand cmd = new SqlCommand();
            cmd.CommandText = "EbauExport_InsertExportDefinition";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@RefPrehShort", Reference);
            cmd.Parameters["@RefPrehShort"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@DestinationSite", DestinationSite);
            cmd.Parameters["@DestinationSite"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@ExportActive", Active);
            cmd.Parameters["@ExportActive"].Direction = ParameterDirection.Input;

            cmd.Parameters.Add("@Return", SqlDbType.Int);
            cmd.Parameters["@Return"].Direction = ParameterDirection.ReturnValue;

            try
            {
                return DBExecuteNonQuery(ref cmd) ? (int)cmd.Parameters["@Return"].Value == 0 : false;
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Error in EBAU_InsertExportDefinition: " + ex.ToString();
                return false;
            }
        }
        public bool EBAU_SendTraceNrToEbau(long SerialNr, long MatNr, int Plant, int Status, DateTime ProductionDate)
        {
            SqlCommand cmd = new SqlCommand();
            cmd.CommandText = "EbauExport_SendTraceNrToEbau";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@SerialNr", SerialNr);
            cmd.Parameters["@SerialNr"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@RefPreh", MatNr);
            cmd.Parameters["@RefPreh"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@Plant", Plant);
            cmd.Parameters["@Plant"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@Status", Status);
            cmd.Parameters["@Status"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@ProductionDate", ProductionDate);
            cmd.Parameters["@ProductionDate"].Direction = ParameterDirection.Input;

            cmd.Parameters.Add("@Return", SqlDbType.Int);
            cmd.Parameters["@Return"].Direction = ParameterDirection.ReturnValue;

            try
            {
                return DBExecuteNonQuery(ref cmd) ? (int)cmd.Parameters["@Return"].Value == 0 : false;
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Error in EBAU_SendTraceNrToEbau: " + ex.ToString();
                return false;
            }
        }
        #endregion

        #region Methods - Config Files

        /// <summary>
        /// <para>Assigns a Config file to a TraceNr previously marked. (for flex lines and rotary EOLs)</para>
        /// <para>Retuns:</para>
        /// <para>1 if the TraceNr was not found</para>
        /// <para>2 if the TraceNr was not marked before the Assign</para>
        /// <para>3 if the version to assign is different from the marked one</para>
        /// <para>0 Assign OK</para>
        /// </summary>
        /// <param name="TraceNr"></param>
        /// <param name="Version"></param>
        /// <returns></returns>
        public int ConfigFiles_AssignVersion(string MaterialNumber, int Version, long TraceNr, int ID_WSJob)
        {
            SqlCommand cmd = new SqlCommand();
            cmd.CommandText = "sp_CFAssignVersion";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@MaterialNumber", MaterialNumber);
            cmd.Parameters["@MaterialNumber"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@Version", Version);
            cmd.Parameters["@Version"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@TraceNr", TraceNr);
            cmd.Parameters["@TraceNr"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@ID_WSJob", ID_WSJob);
            cmd.Parameters["@ID_WSJob"].Direction = ParameterDirection.Input;

            cmd.Parameters.Add("@Return", SqlDbType.Int);
            cmd.Parameters["@Return"].Direction = ParameterDirection.ReturnValue;

            try
            {
                return DBExecuteNonQuery(ref cmd) ? (int)cmd.Parameters["@Return"].Value : -1;
            }
            catch (Exception ex)
            {
                LastErrorDescription = "ConfigFiles_AssignVersion: " + ex.ToString();
                return -1;
            }
        }

        /// <summary>
        /// Checks the availbales versions and gets the config file data to use on the EOL, (returns 0 if ok, 1 if multiple files available, -1 in case of an error).
        /// <para>After execution, the data becomes available through the following read only properties:</para>
        /// <para>[Version] (int) - Version for use on the EOL</para>
        /// <para>[Counter] (int) - If greater than zero, indicates the number os parts remaing for this version</para>
        /// <para>[LastTestedVersion] (int) - The last tested version before this last check</para>
        /// <para>[IsLatestVersion] (bool) - If the field present in [Version] is the latest available in database</para>
        /// <para>[MultipleVersions] (bool) - Indicates if there are multiple versions. The user must choose from a list available in [ConfigFileList]</para>
        /// <para>[ConfigFileList] (DataTable) - The list of Config files</para>
        /// </summary>
        /// <param name="MaterialNumber"></param>
        /// <returns></returns>
        public DataTable ConfigFiles_CheckVersion(string MaterialNumber)
        {
            DataTable ActiveConfigFileList = new DataTable();
            try
            {
                return FillDataTableBySQL("EXEC sp_CFCheckVersion '" + MaterialNumber + "'", sb_DefaultDBConnectionString.ConnectionString);
            }
            catch (Exception ex)
            {
                LastErrorDescription = "ConfigFiles_CheckVersion: " + ex.ToString();
                return null;
            }
        }

        /// <summary>
        /// Transfers the binary file into a string array.
        /// </summary>
        /// <param name="MaterialNumber"></param>
        /// <param name="Version"></param>
        /// <returns></returns>
        public string[] ConfigFiles_GetConfigFile(string MaterialNumber, int Version)
        {
            System.IO.Stream MemStr = new System.IO.MemoryStream();
            System.IO.BinaryWriter fileCreate;
            System.IO.StreamReader fileReader;
            string strCfgFile = "";

            try
            {

                IEnumerable<IDataRecord> readerlist = FillDataReaderBySQL("EXECUTE sp_CFGetConfigFile '" + MaterialNumber + "', " + Version, sb_DefaultDBConnectionString.ConnectionString);
                if (readerlist.Count() > 0)
                {

                    foreach (var reader in readerlist)
                    {

                        byte[] fileData = (byte[])reader[0];

                        fileCreate = new System.IO.BinaryWriter(MemStr);
                        fileCreate.Write((byte[])reader[0]);
                        MemStr.Position = 0;

                        fileReader = new System.IO.StreamReader(MemStr, System.Text.Encoding.GetEncoding("iso-8859-1"));
                        strCfgFile = fileReader.ReadToEnd();
                        fileCreate.Close();
                    }
                }
                return strCfgFile.Split(new string[] { "\r\n" }, StringSplitOptions.None);
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Error in ConfigFiles_GetConfigFile: " + ex.ToString();
                return new string[] { };
            }
        }

        /// <summary>
        /// Returns a list containing the Config files details for a specific material number (the binary file is not included)
        /// </summary>
        /// <param name="MaterialNumber"></param>
        /// <param name="OnlyActive"></param>
        /// <returns></returns>
        public DataTable ConfigFiles_GetConfigFileList(string MaterialNumber, bool OnlyActive)
        {
            return FillDataTableBySQL("EXECUTE sp_CFGetConfigFileList '" + MaterialNumber + "', " + (OnlyActive ? 1 : 0));
        }

        /// <summary>
        /// Transfers the binary file from the database to a given path.
        /// </summary>
        /// <param name="Path"></param>
        /// <param name="MaterialNumber"></param>
        /// <param name="Version"></param>
        /// <returns></returns>
        public bool ConfigFiles_DownloadConfigFile(string Path, string MaterialNumber, int Version)
        {
            System.IO.BinaryWriter fileCreate;
            MaterialNumber = MaterialNumber.Replace("-", "_").Replace("/", "_");

            try
            {

                IEnumerable<IDataRecord> readerlist = FillDataReaderBySQL("EXECUTE sp_CFGetConfigFile '" + MaterialNumber + "', " + Version, sb_DefaultDBConnectionString.ConnectionString);
                if (readerlist.Count() > 0)
                {

                    foreach (var reader in readerlist)
                    {
                        byte[] fileData = (byte[])reader[0];
                        fileCreate = new System.IO.BinaryWriter(System.IO.File.Open(Path, System.IO.FileMode.Create));
                        fileCreate.Write(fileData);
                        fileCreate.Close();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Error in ConfigFiles_DownloadConfigFile: " + ex.ToString();
                return false;
            }
        }
        public int ConfigFiles_GetVersionByTraceNr(long TraceNr)
        {
            SqlCommand cmd = new SqlCommand();
            cmd.CommandText = "sp_CFGetVersionByTraceNr";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@TraceNr", TraceNr);
            cmd.Parameters["@TraceNr"].Direction = ParameterDirection.Input;

            cmd.Parameters.Add("@Return", SqlDbType.Int);
            cmd.Parameters["@Return"].Direction = ParameterDirection.ReturnValue;

            try
            {
                return DBExecuteNonQuery(ref cmd) ? (int)cmd.Parameters["@Return"].Value : -1;
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Error in ConfigFiles_GetVersionByTraceNr: " + ex.ToString();
                return -1;
            }
        }
        public int ConfigFiles_GetLastTestedVersion(string MaterialNumber, out int Version, out int Counter)
        {
            Version = -1;
            Counter = -1;

            SqlCommand cmd = new SqlCommand();
            cmd.CommandText = "sp_CFGetLastTestedVersion";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@MatNr", MaterialNumber);
            cmd.Parameters["@MatNr"].Direction = ParameterDirection.Input;

            cmd.Parameters.Add("@Version", SqlDbType.SmallInt);
            cmd.Parameters["@Version"].Direction = ParameterDirection.Output;

            cmd.Parameters.Add("@Counter", SqlDbType.SmallInt);
            cmd.Parameters["@Counter"].Direction = ParameterDirection.Output;

            cmd.Parameters.Add("@Return", SqlDbType.Int);
            cmd.Parameters["@Return"].Direction = ParameterDirection.ReturnValue;

            try
            {
                if (!DBExecuteNonQuery(ref cmd)) return -1;
                Version = (Int16)cmd.Parameters["@Version"].Value;
                Counter = (Int16)cmd.Parameters["@Counter"].Value;
                return (int)cmd.Parameters["@Return"].Value;
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Error in Trace_AnalysisRelease: " + ex.ToString();
                return -1;
            }
        }

        /// <summary>
        /// Assigns a Config file version to be saved in a TraceNr. (Flex lines and rotary EOLs)
        /// </summary>
        /// <param name="TraceNr"></param>
        /// <param name="ID_WSJob"></param>
        /// <param name="Version"></param>
        /// <returns>int</returns>
        public int ConfigFiles_MarkVersion(string MaterialNumber, int Version, long TraceNr, int ID_WSJob)
        {
            return ConfigFiles_MarkVersion(MaterialNumber, Version, TraceNr, ID_WSJob, false);
        }
        public int ConfigFiles_MarkVersion(string MaterialNumber, int Version, long TraceNr, int ID_WSJob, bool IgnoreLastTest)
        {
            SqlCommand cmd = new SqlCommand();
            cmd.CommandText = "sp_CFMarkVersion";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@TraceNr", TraceNr);
            cmd.Parameters["@TraceNr"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@MaterialNumber", MaterialNumber);
            cmd.Parameters["@MaterialNumber"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@Version", Version);
            cmd.Parameters["@Version"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@ID_WSJob", ID_WSJob);
            cmd.Parameters["@ID_WSJob"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@IgnoreLastTest", IgnoreLastTest);
            cmd.Parameters["@IgnoreLastTest"].Direction = ParameterDirection.Input;

            cmd.Parameters.Add("@Return", SqlDbType.Int);
            cmd.Parameters["@Return"].Direction = ParameterDirection.ReturnValue;

            try
            {
                return DBExecuteNonQuery(ref cmd) ? (int)cmd.Parameters["@Return"].Value : -1;
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Error in ConfigFiles_MarkVersion: " + ex.ToString();
                return -1;
            }
        }

        /// <summary>
        /// Assigns a Config file to a TraceNr imediatelly without the need of previous marking. (for stand alone EOLs)
        /// </summary>
        /// <param name="TraceNr"></param>
        /// <param name="ID_WSJob"></param>
        /// <param name="Version"></param>
        /// <returns></returns>
        public int ConfigFiles_MarkAndAssignVersion(string MaterialNumber, int Version, long TraceNr, int ID_WSJob)
        {
            return ConfigFiles_MarkAndAssignVersion(MaterialNumber, Version, TraceNr, ID_WSJob, false);
        }
        public int ConfigFiles_MarkAndAssignVersion(string MaterialNumber, int Version, long TraceNr, int ID_WSJob, bool IgnoreLastTest)
        {
            SqlCommand cmd = new SqlCommand();
            cmd.CommandText = "sp_CFMarkAndAssignVersion";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@MaterialNumber", MaterialNumber);
            cmd.Parameters["@MaterialNumber"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@Version", Version);
            cmd.Parameters["@Version"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@TraceNr", TraceNr);
            cmd.Parameters["@TraceNr"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@ID_WSJob", ID_WSJob);
            cmd.Parameters["@ID_WSJob"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@IgnoreLastTest", IgnoreLastTest);
            cmd.Parameters["@IgnoreLastTest"].Direction = ParameterDirection.Input;

            cmd.Parameters.Add("@Return", SqlDbType.Int);
            cmd.Parameters["@Return"].Direction = ParameterDirection.ReturnValue;

            try
            {
                return DBExecuteNonQuery(ref cmd) ? (int)cmd.Parameters["@Return"].Value : -1;
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Error in ConfigFiles_MarkAndAssignVersion: " + ex.ToString();
                return -1;
            }
        }

        /// <summary>
        /// Selects the latest Config-file available for the EOL.
        /// </summary>
        /// <param name="MaterialNumber"></param>
        /// <returns>integer</returns>
        public int ConfigFiles_SelectLatestVersion(string MaterialNumber)
        {
            SqlCommand cmd = new SqlCommand();
            cmd.CommandText = "sp_CFSelectLatestVersion";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@MaterialNumber", MaterialNumber);
            cmd.Parameters["@MaterialNumber"].Direction = ParameterDirection.Input;

            cmd.Parameters.Add("@Return", SqlDbType.Int);
            cmd.Parameters["@Return"].Direction = ParameterDirection.ReturnValue;

            try
            {
                return DBExecuteNonQuery(ref cmd) ? (int)cmd.Parameters["@Return"].Value : -1;
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Error in ConfigFiles_SelectLatestVersion: " + ex.ToString();
                return -1;
            }
        }

        /// <summary>
        /// Selects a Config-file for the EOL, except the latest, for use with a counter.
        /// </summary>
        /// <param name="MaterialNumber"></param>
        /// <param name="Version"></param>
        /// <param name="Counter"></param>
        /// <returns>int</returns>
        public int ConfigFiles_SelectVersionWithCounter(string MaterialNumber, int Version, int Counter)
        {
            SqlCommand cmd = new SqlCommand();
            cmd.CommandText = "sp_CFSelectVersionWithCounter";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@MaterialNumber", MaterialNumber);
            cmd.Parameters["@MaterialNumber"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@Version", Version);
            cmd.Parameters["@Version"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@Counter", Counter);
            cmd.Parameters["@Counter"].Direction = ParameterDirection.Input;

            cmd.Parameters.Add("@Return", SqlDbType.Int);
            cmd.Parameters["@Return"].Direction = ParameterDirection.ReturnValue;

            try
            {
                return DBExecuteNonQuery(ref cmd) ? (int)cmd.Parameters["@Return"].Value : -1;
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Error in ConfigFiles_SelectVersionWithCounter: " + ex.ToString();
                return -1;
            }
        }

        /// <summary>
        /// Decrements the Counter when a part is tested OK. (Not needed for EOL)
        /// </summary>
        /// <param name="MaterialNumber"></param>
        /// <returns>integer</returns>
        public int ConfigFiles_UpdateVersionCounter(string MaterialNumber, bool Decrement, int NewValue)
        {
            SqlCommand cmd = new SqlCommand();
            cmd.CommandText = "sp_CFUpdateVersionCounter";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@MaterialNumber", MaterialNumber);
            cmd.Parameters["@MaterialNumber"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@Decrement", Decrement);
            cmd.Parameters["@Decrement"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@NewValue", NewValue);
            cmd.Parameters["@NewValue"].Direction = ParameterDirection.Input;

            cmd.Parameters.Add("@Return", SqlDbType.Int);
            cmd.Parameters["@Return"].Direction = ParameterDirection.ReturnValue;

            try
            {
                return DBExecuteNonQuery(ref cmd) ? (int)cmd.Parameters["@Return"].Value : -1;
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Error in ConfigFiles_UpdateVersionCounter: " + ex.ToString();
                return -1;
            }
        }

        #endregion

        #region Methods - Traceability Error Messages
        public bool SetSystemLanguage(Language lang)
        {
            SystemLanguage = lang;
            return Errors_LoadTraceErrorsLanguage();
        }
        public bool Errors_LoadTraceErrorsLanguage()
        {
            TraceErrorMessages = new List<TraceErrorMessage>();
            try
            {

                IEnumerable<IDataRecord> readerlist = FillDataReaderBySQL("EXEC Trace_GetTraceErrorDescriptions '" + SystemLanguage.ToString() + "'", sb_ProdGenericsConnectionString.ConnectionString);
                if (readerlist.Count() > 0)
                {

                    foreach (var reader in readerlist)
                    {
                        TraceErrorMessages.Add(new TraceErrorMessage
                        {
                            TraceErrorCode = (byte)reader["TraceErrorCode"],
                            TraceErrorDescription = reader["TraceErrorDescription"] == DBNull.Value ? "No error description!" : (string)reader["TraceErrorDescription"]
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Error in Errors_LoadTraceErrorsLanguage: " + ex.ToString();
                return false;
            }
            return true;
        }
        public string Errors_GetTraceErrorDescription(int ErrorNr)
        {
            if (TraceErrorMessages.Where(x => x.TraceErrorCode == ErrorNr).Count() == 0) return "";
            return TraceErrorMessages.Where(x => x.TraceErrorCode == ErrorNr).FirstOrDefault().TraceErrorDescription;
        }
        #endregion

        #region Methods - Languages
        public bool Languages_LoadMessages(int ID_SubWS)
        {
            string IDFieldName = "ID_SubWS";
            Messages = new List<Message>();
            try
            {

                IEnumerable<IDataRecord> readerlist = FillDataReaderBySQL("EXEC GetLangs " + ID_SubWS, sb_DefaultDBConnectionString.ConnectionString);

                foreach (var reader in readerlist)
                {

                    for (int i = 0; i < reader.FieldCount; i++) { if (reader.GetName(i).ToLower().Equals("id_test")) { IDFieldName = "ID_Test"; break; } }

                    Messages.Add(new Message
                    {
                        ID_Msg = (int)reader["ID_Msg"],
                        ID_SubWS = (int)reader[IDFieldName],
                        DefaultLanguage = reader["DefaultLanguage"] == DBNull.Value ? "" : (string)reader["DefaultLanguage"],
                        English = reader["English"] == DBNull.Value ? "" : (string)reader["English"],
                        Portuguese = reader["Portuguese"] == DBNull.Value ? "" : (string)reader["Portuguese"],
                        German = reader["German"] == DBNull.Value ? "" : (string)reader["German"],
                        Spanish = reader["Spanish"] == DBNull.Value ? "" : (string)reader["Spanish"],
                        Romanian = reader["Romanian"] == DBNull.Value ? "" : (string)reader["Romanian"],
                        Chinese = reader["Chinese"] == DBNull.Value ? "" : (string)reader["Chinese"],
                    });
                }
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Error in Languages_LoadMessages: " + ex.ToString();
                return false;
            }
            return true;
        }
        public string Languages_GetMessage(string Msg, int ID_SubWS)
        {
            try
            {

                if (Language_AutoInsertNewMessages && Messages.Where(x => x.DefaultLanguage.ToLower().Replace(" ", "").Trim().Equals(Msg.ToLower().Replace(" ", "").Trim()) && x.ID_SubWS == ID_SubWS).Count() == 0)
                {
                    Languages_InsertNewMsg(Msg, ID_SubWS);
                    Languages_LoadMessages(ID_SubWS);
                }
                Message m = Messages.Where(x => x.DefaultLanguage.ToLower().Replace(" ", "").Trim().Equals(Msg.ToLower().Replace(" ", "").Trim()) && x.ID_SubWS == ID_SubWS).First();
                if (m == null) return Language_AutoInsertNewMessages ? "" : Msg;

                switch (SystemLanguage)
                {
                    case Language.PT:
                        return m.Portuguese;
                    case Language.EN:
                        return m.English;
                    case Language.DE:
                        return m.German;
                    case Language.RO:
                        return m.Romanian;
                    case Language.ES:
                        return m.Spanish;
                    case Language.CN:
                        return m.Chinese;
                    default:
                        return "";

                }
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Error in Languages_GetMessage" + ex.ToString();
                return "";
            }
        }
        protected bool Languages_InsertNewMsg(string Message, int ID_SubWS)
        {
            SqlCommand cmd = new SqlCommand();
            cmd.CommandText = "InsertNewMsg";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@Msg", Message);
            cmd.Parameters["@Msg"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@ID_SubWS", ID_SubWS);
            cmd.Parameters["@ID_SubWS"].Direction = ParameterDirection.Input;

            cmd.Parameters.Add("@Return", SqlDbType.Int);
            cmd.Parameters["@Return"].Direction = ParameterDirection.ReturnValue;

            try
            {
                return DBExecuteNonQuery(ref cmd) ? (int)cmd.Parameters["@Return"].Value == 0 : false;
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Error in Languages_InsertNewMsg: " + ex.ToString();
                return false;
            }
        }
        public bool Languages_UpdateMsg(string OldMessage, string NewMessage, int ID_SubWS)
        {
            SqlCommand cmd = new SqlCommand();
            cmd.CommandText = "UpdateMsg";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@OldMessage", OldMessage);
            cmd.Parameters["@OldMessage"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@NewMessage", NewMessage);
            cmd.Parameters["@NewMessage"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@ID_SubWS", ID_SubWS);
            cmd.Parameters["@ID_SubWS"].Direction = ParameterDirection.Input;

            cmd.Parameters.Add("@Return", SqlDbType.Int);
            cmd.Parameters["@Return"].Direction = ParameterDirection.ReturnValue;

            try
            {
                return DBExecuteNonQuery(ref cmd) ? (int)cmd.Parameters["@Return"].Value == 0 : false;
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Error in Languages_UpdateMsg: " + ex.ToString();
                return false;
            }
        }
        #endregion

        #region Methods - Critical Errors
        public List<PartErrorConfiguration> Critical_GetErrorConfiguration(int ID_SubWS, long TraceNr)
        {
            List<PartErrorConfiguration> PartErrorConfigurations = new List<PartErrorConfiguration>();
            try
            {
                IEnumerable<IDataRecord> readerlist = FillDataReaderBySQL("EXECUTE DeviceErrorGetErrorConfigurationByPart " + ID_SubWS + "," + TraceNr, sb_DefaultDBConnectionString.ConnectionString);

                foreach (var reader in readerlist)
                {
                    PartErrorConfigurations.Add(new PartErrorConfiguration
                    {
                        ID_ErrorConfiguration = (int)reader["ID_ErrorConfiguration"],
                        ID_WSJob = (int)reader["ID_WSJob"],
                        ID_DeviceResultTestObject = (int)reader["ID_DeviceResultTestObject"],
                        ID_DeviceResultTestCharacteristic = (int)reader["ID_DeviceResultTestCharacteristic"],
                        Actions = (string)reader["Actions"],
                        Causes = (string)reader["Causes"],
                        ErrorCode = (string)reader["ErrorCode"],
                        TreatAsCritical = (bool)reader["TreatAsCritical"],
                    });
                }
                return PartErrorConfigurations;
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Error in LoadStationData: " + ex.ToString();
                return null;
            }
        }
        #endregion

        #region Methods - Users
        public bool Users_GetAppActiveUsers(string appName, string connectionstring)
        {
            AppUsers = new List<AppUser>();
            try
            {
                IEnumerable<IDataRecord> readerlist = FillDataReaderBySQL("EXECUTE Login_GetAppActiveUsers '" + appName + "'", connectionstring);
                foreach (var reader in readerlist)
                {
                    AppUsers.Add(new AppUser
                    {
                        Identification = (string)reader["Identification"],
                        UserName = (string)reader["UserName"],
                        Department = (string)reader["Department"],
                        ID_User = (int)reader["ID_User"],
                        AccessMask = (int)reader["AccessMask"],
                        Psw = (string)reader["Psw"],
                    });
                }
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Error in Users_GetAppActiveUsers: " + ex.ToString();
                return false;
            }
            return true;
        }
        public bool Users_GetAppActiveUsers(string appName)
        {
            return Users_GetAppActiveUsers(appName, sb_ProdGenericsConnectionString.ConnectionString);
        }
        public bool Users_CheckUserPassword(int userID, string userPsw)
        {
            AuthenticatedUser = null;
            if (AppUsers == null) return false;
            if (AppUsers.Where(x => x.ID_User == userID).Count() == 0) return false;
            if (!AppUsers.Where(x => x.ID_User == userID).FirstOrDefault().Psw.Equals(EncryptPsw(userPsw))) return false;

            AuthenticatedUser = AppUsers.Where(x => x.ID_User == userID).FirstOrDefault();
            return true;
        }
        private string EncryptPsw(string pswToEncrypt)
        {
            string encryptedPsw = "";
            char[] arrayOfChar = pswToEncrypt.ToCharArray();

            System.Text.Encoding Enc = System.Text.Encoding.GetEncoding(1252);
            byte[] arrayOfBytes = Enc.GetBytes(arrayOfChar);

            for (int i = 0; i < arrayOfBytes.Length; i++)
                arrayOfBytes[i] = (byte)(arrayOfBytes[i] + 80);

            arrayOfChar = Enc.GetChars(arrayOfBytes);

            for (int i = 0; i < arrayOfChar.Length; i++)
                encryptedPsw = encryptedPsw + arrayOfChar[i].ToString();
            return encryptedPsw;
        }
        #endregion

        #region Methods - References
        public bool Refs_LoadReferences(string ReferencesSQLView)
        {
            References = new List<Reference>();
            try
            {
                IEnumerable<IDataRecord> readerlist = FillDataReaderBySQL("SELECT * FROM [" + ReferencesSQLView + "]", sb_DefaultDBConnectionString.ConnectionString);

                foreach (var reader in readerlist)
                {
                    References.Add(new Reference
                    {
                        IDRef = (int)reader["ID_Ref"],
                        IDRefProfile = reader["ID_RefProfile"] == DBNull.Value ? -1 : (int)reader["ID_RefProfile"],
                        IDRefSeqGroup = reader["ID_RefSeqGroup"] == DBNull.Value ? -1 : (int)reader["ID_RefSeqGroup"],
                        ExtraCode = reader["ExtraCode"] == DBNull.Value ? "" : (string)reader["ExtraCode"],
                        RefPreh = (string)reader["RefPreh"],
                        RefDescription = reader["RefDescription"] == DBNull.Value ? "" : (string)reader["RefDescription"],
                        RefShortType = reader["RefShortType"] == DBNull.Value ? "" : (string)reader["RefShortType"],
                        Active = (bool)reader["Active"],
                    });
                }
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Error in Refs_GetRefs: " + ex.ToString();
                return false;
            }
            return true;
        }
        #endregion

        #region Methods - Traceability
        public int Trace_AnalisysBlock(long TraceNr, int OperatorID)
        {
            SqlCommand cmd = new SqlCommand("Trace_AnalysisBlock");
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@OpID", OperatorID);
            cmd.Parameters["@OpID"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@TNr", TraceNr);
            cmd.Parameters["@TNr"].Direction = ParameterDirection.Input;

            cmd.Parameters.Add("@Return", SqlDbType.Int);
            cmd.Parameters["@Return"].Direction = ParameterDirection.ReturnValue;

            try
            {
                return DBExecuteNonQuery(ref cmd) ? (int)cmd.Parameters["@Return"].Value : -1;
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Trace_AnalisysBlock: " + ex.ToString();
                return -1;
            }
        }
        public int Trace_AnalisysRelease(long TraceNr, int OperatorID, int WSCenter)
        {
            SqlCommand cmd = new SqlCommand("Trace_AnalysisRelease");
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@OpID", OperatorID);
            cmd.Parameters["@OpID"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@TNr", TraceNr);
            cmd.Parameters["@TNr"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@WSCRel", WSCenter);
            cmd.Parameters["@WSCRel"].Direction = ParameterDirection.Input;

            cmd.Parameters.Add("@res", SqlDbType.Int);
            cmd.Parameters["@res"].Direction = ParameterDirection.ReturnValue;

            try
            {
                return DBExecuteNonQuery(ref cmd) ? (int)cmd.Parameters["@Return"].Value : -1;
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Trace_AnalysisRelease: " + ex.ToString();
                return -1;
            }
        }
        public int Trace_AssignRef(long TraceNr, string RefPreh, int IDWS, int SubWS)
        {
            SqlCommand cmd = new SqlCommand("Trace_AssignRef");
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@TNr", TraceNr);
            cmd.Parameters["@TNr"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@FinalRef", RefPreh);
            cmd.Parameters["@FinalRef"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@WSID", IDWS);
            cmd.Parameters["@WSID"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@SubWS", SubWS);
            cmd.Parameters["@SubWS"].Direction = ParameterDirection.Input;

            cmd.Parameters.Add("@Return", SqlDbType.Int);
            cmd.Parameters["@Return"].Direction = ParameterDirection.ReturnValue;

            try
            {
                return DBExecuteNonQuery(ref cmd) ? (int)cmd.Parameters["@Return"].Value : -1;
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Trace_AssignRef: " + ex.ToString();
                return -1;
            }
        }
        public int Trace_CheckAssembly(long TraceNrParent, long TraceNrChild, int IDWS, int SubWS)
        {
            SqlCommand cmd = new SqlCommand("Trace_CheckAssembly");
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@TNrParent", TraceNrParent);
            cmd.Parameters["@TNrParent"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@TNrChild", TraceNrChild);
            cmd.Parameters["@TNrChild"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@WSID", IDWS);
            cmd.Parameters["@WSID"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@SubWS", SubWS);
            cmd.Parameters["@SubWS"].Direction = ParameterDirection.Input;

            cmd.Parameters.Add("@Return", SqlDbType.Int);
            cmd.Parameters["@Return"].Direction = ParameterDirection.ReturnValue;

            try
            {
                return DBExecuteNonQuery(ref cmd) ? (int)cmd.Parameters["@Return"].Value : -1;
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Trace_CheckAssembly: " + ex.ToString();
                return -1;
            }
        }
        public int Trace_CheckAssemblyByRefPreh(string RefParent, string RefChild, int IDWS, int SubWS)
        {
            SqlCommand cmd = new SqlCommand("Trace_CheckAssemblyByRef");
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@RefParent", RefParent);
            cmd.Parameters["@RefParent"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@RefChild", RefChild);
            cmd.Parameters["@RefChild"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@WSID", IDWS);
            cmd.Parameters["@WSID"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@SubWS", SubWS);
            cmd.Parameters["@SubWS"].Direction = ParameterDirection.Input;

            cmd.Parameters.Add("@Return", SqlDbType.Int);
            cmd.Parameters["@Return"].Direction = ParameterDirection.ReturnValue;

            try
            {
                return DBExecuteNonQuery(ref cmd) ? (int)cmd.Parameters["@Return"].Value : -1;
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Trace_CheckAssemblyByRef: " + ex.ToString();
                return -1;
            }
        }
        public int Trace_CheckLaser(long TraceNr)
        {
            SqlCommand cmd = new SqlCommand("CheckLaser");
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@TNr", TraceNr);
            cmd.Parameters["@TNr"].Direction = ParameterDirection.Input;

            cmd.Parameters.Add("@Return", SqlDbType.Int);
            cmd.Parameters["@Return"].Direction = ParameterDirection.ReturnValue;

            try
            {
                return DBExecuteNonQuery(ref cmd) ? (int)cmd.Parameters["@Return"].Value : -1;
            }
            catch (Exception ex)
            {
                LastErrorDescription = "CheckLaser: " + ex.ToString();
                return -1;
            }
        }
        public int Trace_CheckTraceNr(long TraceNr, string RefPreh, int IDWS, int SubWS)
        {
            SqlCommand cmd = new SqlCommand("Trace_CheckTraceNr");
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@TNr", TraceNr);
            cmd.Parameters["@TNr"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@FinalRef", RefPreh);
            cmd.Parameters["@FinalRef"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@WSID", IDWS);
            cmd.Parameters["@WSID"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@SubWS", SubWS);
            cmd.Parameters["@SubWS"].Direction = ParameterDirection.Input;

            cmd.Parameters.Add("@Return", SqlDbType.Int);
            cmd.Parameters["@Return"].Direction = ParameterDirection.ReturnValue;

            try
            {
                return DBExecuteNonQuery(ref cmd) ? (int)cmd.Parameters["@Return"].Value : -1;
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Trace_CheckTraceNr: " + ex.ToString();
                return -1;
            }
        }
        public int Trace_CheckUpgradeRef(long TraceNr, string FinalRef, int IDWS, int SubWS)
        {
            SqlCommand cmd = new SqlCommand("Trace_CheckUpgradeRef");
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@TNr", TraceNr);
            cmd.Parameters["@TNr"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@FinalRef", FinalRef);
            cmd.Parameters["@FinalRef"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@WSID", IDWS);
            cmd.Parameters["@WSID"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@SubWS", SubWS);
            cmd.Parameters["@SubWS"].Direction = ParameterDirection.Input;

            cmd.Parameters.Add("@Return", SqlDbType.Int);
            cmd.Parameters["@Return"].Direction = ParameterDirection.ReturnValue;

            try
            {
                return DBExecuteNonQuery(ref cmd) ? (int)cmd.Parameters["@Return"].Value : -1;
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Trace_CheckUpgradeRef: " + ex.ToString();
                return -1;
            }
        }
        public int Trace_DisassembleUnit(long TraceNr, int OperatorID)
        {
            SqlCommand cmd = new SqlCommand("Trace_DisassembleUnit");
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@OpID", OperatorID);
            cmd.Parameters["@OpID"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@TNr", TraceNr);
            cmd.Parameters["@TNr"].Direction = ParameterDirection.Input;

            cmd.Parameters.Add("@Return", SqlDbType.Int);
            cmd.Parameters["@Return"].Direction = ParameterDirection.ReturnValue;

            try
            {
                return DBExecuteNonQuery(ref cmd) ? (int)cmd.Parameters["@Return"].Value : -1;
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Trace_DisassembleUnit: " + ex.ToString();
                return -1;
            }
        }
        public int Trace_DisassembleChild(long TraceNr, int OperatorID)
        {
            SqlCommand cmd = new SqlCommand("Trace_DisassembleChild");
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@OpID", OperatorID);
            cmd.Parameters["@OpID"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@TNr", TraceNr);
            cmd.Parameters["@TNr"].Direction = ParameterDirection.Input;

            cmd.Parameters.Add("@Return", SqlDbType.Int);
            cmd.Parameters["@Return"].Direction = ParameterDirection.ReturnValue;

            try
            {
                return DBExecuteNonQuery(ref cmd) ? (int)cmd.Parameters["@Return"].Value : -1;
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Trace_DisassembleChild: " + ex.ToString();
                return -1;
            }
        }
        public string Trace_GetPrehRefFromTraceNr(long TraceNr)
        {
            SqlCommand cmd = new SqlCommand("Trace_GetRefByTraceNr");
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@TNr", TraceNr);
            cmd.Parameters["@TNr"].Direction = ParameterDirection.Input;

            cmd.Parameters.Add("@Return", SqlDbType.Int);
            cmd.Parameters["@Return"].Direction = ParameterDirection.ReturnValue;

            try
            {
                object RefPreh = DBExecuteScalar(cmd);
                return RefPreh == null ? "" : RefPreh.ToString();
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Trace_GetRefByTraceNr: " + ex.ToString();
                return "";
            }
        }
        public int Trace_GetIDFromTraceNr(long TraceNr)
        {
            SqlCommand cmd = new SqlCommand("GetIDFromTraceNr");
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@TraceNr", TraceNr);
            cmd.Parameters["@TraceNr"].Direction = ParameterDirection.Input;

            cmd.Parameters.Add("@Return", SqlDbType.Int);
            cmd.Parameters["@Return"].Direction = ParameterDirection.ReturnValue;

            try
            {
                object ID_Ref = DBExecuteScalar(cmd);
                return ID_Ref == null ? -1 : (int)ID_Ref;
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Trace_GetIDFromTraceNr: " + ex.ToString();
                return -1;
            }
        }
        public int Trace_SaveInfo(int JobID, string InfoType, string Info)
        {
            SqlCommand cmd = new SqlCommand("Trace_SaveInfo");
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@JobID", JobID);
            cmd.Parameters["@JobID"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@InfoType", InfoType);
            cmd.Parameters["@InfoType"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@Info", Info);
            cmd.Parameters["@Info"].Direction = ParameterDirection.Input;

            cmd.Parameters.Add("@Return", SqlDbType.Int);
            cmd.Parameters["@Return"].Direction = ParameterDirection.ReturnValue;

            try
            {
                return DBExecuteNonQuery(ref cmd) ? (int)cmd.Parameters["@Return"].Value : -1;
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Trace_SaveInfo: " + ex.ToString();
                return -1;
            }
        }
        public int Trace_SaveAssembly(long TraceNrParent, long TraceNrChild, int JobID)
        {
            SqlCommand cmd = new SqlCommand("Trace_SaveAssembly");
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@TNrParent", TraceNrParent);
            cmd.Parameters["@TNrParent"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@TNrChild", TraceNrChild);
            cmd.Parameters["@TNrChild"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@WSJobID", JobID);
            cmd.Parameters["@WSJobID"].Direction = ParameterDirection.Input;

            cmd.Parameters.Add("@Return", SqlDbType.Int);
            cmd.Parameters["@Return"].Direction = ParameterDirection.ReturnValue;

            try
            {
                return DBExecuteNonQuery(ref cmd) ? (int)cmd.Parameters["@Return"].Value : -1;
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Trace_SaveAssembly: " + ex.ToString();
                return -1;
            }
        }
        public int Trace_UpgradeRef(long TraceNr, string FinalRef, int IDWS, int SubWS, int JobID)
        {
            SqlCommand cmd = new SqlCommand("Trace_UpgradeRef");
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@TNr", TraceNr);
            cmd.Parameters["@TNr"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@FinalRef", FinalRef);
            cmd.Parameters["@FinalRef"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@WSID", IDWS);
            cmd.Parameters["@WSID"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@SubWS", SubWS);
            cmd.Parameters["@SubWS"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@JobID", JobID);
            cmd.Parameters["@JobID"].Direction = ParameterDirection.Input;

            cmd.Parameters.Add("@Return", SqlDbType.Int);
            cmd.Parameters["@Return"].Direction = ParameterDirection.ReturnValue;

            try
            {
                return DBExecuteNonQuery(ref cmd) ? (int)cmd.Parameters["@Return"].Value : -1;
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Trace_UpgradeRef: " + ex.ToString();
                return -1;
            }
        }
        public int Trace_JobStart(long TraceNr, int IDWS, int SubWS, ref int JobID)
        {
            //Clears JobID:
            JobID = 0;

            //Clears the error list:
            TestErrorResults = new List<TestErrorResult>();

            SqlCommand cmd = new SqlCommand("Trace_JobStart");
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@TNr", TraceNr);
            cmd.Parameters["@TNr"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@WSID", IDWS);
            cmd.Parameters["@WSID"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@SubWS", SubWS);
            cmd.Parameters["@SubWS"].Direction = ParameterDirection.Input;

            cmd.Parameters.Add("@JobID", SqlDbType.Int);
            cmd.Parameters["@JobID"].Direction = ParameterDirection.Output;

            cmd.Parameters.Add("@Return", SqlDbType.Int);
            cmd.Parameters["@Return"].Direction = ParameterDirection.ReturnValue;

            try
            {
                if (!DBExecuteNonQuery(ref cmd)) return -1;
                JobID = (int)cmd.Parameters["@JobID"].Value;
                return (int)cmd.Parameters["@Return"].Value;
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Trace_JobStart: " + ex.ToString();
                return -1;
            }
        }
        public int Trace_JobEnd(int JobID, byte JobResult)
        {
            SqlCommand cmd = new SqlCommand("Trace_JobEnd");
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@JobID", JobID);
            cmd.Parameters["@JobID"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@JobResult", JobResult);
            cmd.Parameters["@JobResult"].Direction = ParameterDirection.Input;

            cmd.Parameters.Add("@Return", SqlDbType.Int);
            cmd.Parameters["@Return"].Direction = ParameterDirection.ReturnValue;

            try
            {
                return DBExecuteNonQuery(ref cmd) ? (int)cmd.Parameters["@Return"].Value : -1;
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Trace_JobEnd: " + ex.ToString();
                return -1;
            }
        }
        public int Trace_JobEndWithErrorDetails(int JobID, byte JobResult, int ErrorCode, string ErrorDetails)
        {
            SqlCommand cmd = new SqlCommand("Trace_JobEndWithErrorDetails");
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@JobID", JobID);
            cmd.Parameters["@JobID"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@JobResult", JobResult);
            cmd.Parameters["@JobResult"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@ErrorCode", ErrorCode);
            cmd.Parameters["@ErrorCode"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@ErrorDetails", ErrorDetails);
            cmd.Parameters["@ErrorDetails"].Direction = ParameterDirection.Input;

            cmd.Parameters.Add("@Return", SqlDbType.Int);
            cmd.Parameters["@Return"].Direction = ParameterDirection.ReturnValue;

            try
            {
                return DBExecuteNonQuery(ref cmd) ? (int)cmd.Parameters["@Return"].Value : -1;
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Trace_JobEndWithErrorDetails: " + ex.ToString();
                return -1;
            }
        }
        public int Trace_JobSaveError(int JobID, int ErrorCode, string ErrorDetails)
        {
            SqlCommand cmd = new SqlCommand("Trace_JobSaveError");
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@JobID", JobID);
            cmd.Parameters["@JobID"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@ErrorCode", ErrorCode);
            cmd.Parameters["@ErrorCode"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@ErrorDetails", ErrorDetails);
            cmd.Parameters["@ErrorDetails"].Direction = ParameterDirection.Input;

            cmd.Parameters.Add("@Return", SqlDbType.Int);
            cmd.Parameters["@Return"].Direction = ParameterDirection.ReturnValue;

            try
            {
                return DBExecuteNonQuery(ref cmd) ? (int)cmd.Parameters["@Return"].Value : -1;
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Trace_JobSaveError: " + ex.ToString();
                return -1;
            }
        }
        public Reference Trace_GetRefDataByIDRef(int IDRef)
        {
            SqlCommand cmd = new SqlCommand("Trace_GetRefDataByIDRef");
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@ID_Ref", IDRef);
            cmd.Parameters["@ID_Ref"].Direction = ParameterDirection.Input;

            cmd.Parameters.Add("@Return", SqlDbType.Int);
            cmd.Parameters["@Return"].Direction = ParameterDirection.ReturnValue;

            Reference Ref = new Reference();
            try
            {
                IEnumerable<IDataRecord> readerlist = FillDataReaderBySQL(cmd, sb_DefaultDBConnectionString.ConnectionString);
                foreach (var reader in readerlist)
                {
                    Ref = new Reference
                    {
                        IDRef = IDRef,
                        IDRefProfile = reader["ID_RefProfile"] == DBNull.Value ? -1 : (int)reader["ID_RefProfile"],
                        IDRefSeqGroup = reader["ID_RefSeqGroup"] == DBNull.Value ? -1 : (int)reader["ID_RefSeqGroup"],
                        ExtraCode = reader["ExtraCode"] == DBNull.Value ? "" : (string)reader["ExtraCode"],
                        RefPreh = (string)reader["RefPreh"],
                        RefDescription = reader["RefDescription"] == DBNull.Value ? "" : (string)reader["RefDescription"],
                        RefShortType = reader["RefShortType"] == DBNull.Value ? "" : (string)reader["RefShortType"],
                        Active = (bool)reader["Active"],
                    };
                }
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Error in Refs_GetRefs: " + ex.ToString();
                return null;
            }
            return Ref.IDRef > 0 ? Ref : null;
        }
        public int Trace_GetIDRefFromRefPreh(string RefPreh)
        {
            SqlCommand cmd = new SqlCommand("Trace_GetIDRefFromRefPreh");
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@RefPreh", RefPreh);
            cmd.Parameters["@RefPreh"].Direction = ParameterDirection.Input;

            cmd.Parameters.Add("@Return", SqlDbType.Int);
            cmd.Parameters["@Return"].Direction = ParameterDirection.ReturnValue;

            try
            {
                object ID_Ref = DBExecuteScalar(cmd);
                return (ID_Ref == null ? -1 : (int)ID_Ref);
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Trace_GetIDRefFromRefPreh: " + ex.ToString();
                return -1;
            }
        }
        public int Trace_GetIDSubWSFromIDWSAndSubWS(int IDWS, int SubWS)
        {
            SqlCommand cmd = new SqlCommand("Trace_GetIDSubwsFromIDWSAndSubWS");
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@IDWS", IDWS);
            cmd.Parameters["@IDWS"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@SubWS", SubWS);
            cmd.Parameters["@SubWS"].Direction = ParameterDirection.Input;

            cmd.Parameters.Add("@Return", SqlDbType.Int);
            cmd.Parameters["@Return"].Direction = ParameterDirection.ReturnValue;

            try
            {
                object ID_SubWS = DBExecuteScalar(cmd);
                return (ID_SubWS == null ? -1 : (int)ID_SubWS);
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Trace_GetIDSubWSFromIDWSAndSubWS: " + ex.ToString();
                return -1;
            }
        }
        public int Trace_RFIDCheckBatchLimit(long TraceNr)
        {
            SqlCommand cmd = new SqlCommand("Trace_RFIDCheckBatchLimit");
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@TNr", TraceNr);
            cmd.Parameters["@TNr"].Direction = ParameterDirection.Input;

            cmd.Parameters.Add("@Return", SqlDbType.Int);
            cmd.Parameters["@Return"].Direction = ParameterDirection.ReturnValue;

            try
            {
                object ID_Ref = DBExecuteScalar(cmd);
                return ID_Ref == null ? -1 : (int)ID_Ref;
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Trace_RFIDCheckBatchLimit: " + ex.ToString();
                return -1;
            }
        }
        public int Trace_RFIDImport(long TraceNr)
        {
            SqlCommand cmd = new SqlCommand("Trace_RFIDImport");
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@TNr", TraceNr);
            cmd.Parameters["@TNr"].Direction = ParameterDirection.Input;

            cmd.Parameters.Add("@Return", SqlDbType.Int);
            cmd.Parameters["@Return"].Direction = ParameterDirection.ReturnValue;

            try
            {
                return DBExecuteNonQuery(ref cmd) ? (int)cmd.Parameters["@Return"].Value : 1;
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Trace_RFIDImport: " + ex.ToString();
                return -1;
            }
        }
        public int Trace_RFIDGetTraceNrByTag(string TagSerial, out long TraceNr, out int Counter, out int Finished, out bool TrayLastCycle)
        {
            Counter = 0;
            Finished = 0;
            TrayLastCycle = false;
            TraceNr = 0;

            SqlCommand cmd = new SqlCommand("RFIDGetTraceNrByTag");
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@TagSerial", TagSerial);
            cmd.Parameters["@TagSerial"].Direction = ParameterDirection.Input;

            cmd.Parameters.Add("@TNr");
            cmd.Parameters["@TNr"].Direction = ParameterDirection.Output;

            cmd.Parameters.Add("@Counter", SqlDbType.Int);
            cmd.Parameters["@Counter"].Direction = ParameterDirection.Output;

            cmd.Parameters.Add("@Finished", SqlDbType.Int);
            cmd.Parameters["@Finished"].Direction = ParameterDirection.Output;

            cmd.Parameters.Add("@TrayLastCycle", SqlDbType.Bit);
            cmd.Parameters["@TrayLastCycle"].Direction = ParameterDirection.Output;

            cmd.Parameters.Add("@Return", SqlDbType.Int);
            cmd.Parameters["@Return"].Direction = ParameterDirection.ReturnValue;

            try
            {
                if (!DBExecuteNonQuery(ref cmd)) return -1;
                TraceNr = (int)cmd.Parameters["@TNr"].Value;
                Counter = (int)cmd.Parameters["@Counter"].Value;
                Finished = (int)cmd.Parameters["@Finished"].Value;
                TrayLastCycle = (bool)cmd.Parameters["@TrayLastCycle"].Value;
                return (int)cmd.Parameters["@Return"].Value;
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Trace_RFIDGetTraceNrByTag: " + ex.ToString();
                return -1;
            }
        }
        public int Trace_RFIDGetRefByTraceNr(long TraceNr, out int IDRef, out string RefPreh, out string RefShortType, out string RefDescription, out string TrayCapacity, out string TrayCycleLimit)
        {
            IDRef = 0;
            RefPreh = "";
            RefShortType = "";
            RefDescription = "";
            TrayCapacity = "";
            TrayCycleLimit = "";

            SqlCommand cmd = new SqlCommand("RFIDGetRefByTraceNr");
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@TNr", TraceNr);
            cmd.Parameters["@TNr"].Direction = ParameterDirection.Input;

            cmd.Parameters.Add("@IDRef", SqlDbType.Int);
            cmd.Parameters["@IDRef"].Direction = ParameterDirection.Output;

            cmd.Parameters.Add("@RefPreh", SqlDbType.VarChar);
            cmd.Parameters["@RefPreh"].Direction = ParameterDirection.Output;

            cmd.Parameters.Add("@RefShortType", SqlDbType.VarChar);
            cmd.Parameters["@RefShortType"].Direction = ParameterDirection.Output;

            cmd.Parameters.Add("@RefDescription", SqlDbType.VarChar);
            cmd.Parameters["@RefDescription"].Direction = ParameterDirection.Output;

            cmd.Parameters.Add("@TrayCapacity", SqlDbType.VarChar);
            cmd.Parameters["@TrayCapacity"].Direction = ParameterDirection.Output;

            cmd.Parameters.Add("@TrayCycleLimit", SqlDbType.VarChar);
            cmd.Parameters["@TrayCycleLimit"].Direction = ParameterDirection.Output;

            cmd.Parameters.Add("@Return", SqlDbType.Int);
            cmd.Parameters["@Return"].Direction = ParameterDirection.ReturnValue;

            try
            {
                if (!DBExecuteNonQuery(ref cmd)) return -1;
                IDRef = (int)cmd.Parameters["@IDRef"].Value;
                RefPreh = cmd.Parameters["@RefPreh"].Value.ToString();
                RefShortType = cmd.Parameters["@RefShortType"].Value.ToString();
                RefDescription = cmd.Parameters["@RefDescription"].Value.ToString();
                TrayCapacity = cmd.Parameters["@TrayCapacity"].Value.ToString();
                TrayCycleLimit = cmd.Parameters["@TrayCycleLimit"].Value.ToString();
                return (int)cmd.Parameters["@Return"].Value;
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Trace_RFIDGetRefByTraceNr: " + ex.ToString();
                return -1;
            }
        }
        public List<TraceNrHistoryItem> Trace_GetHistoryByTraceNr(long TraceNr, int IDWS, int SubWS, out bool IsSample)
        {
            IsSample = Samples_CheckSample(TraceNr, IDWS, SubWS);
            return Trace_GetHistoryByTraceNr(TraceNr);
        }
        public List<TraceNrHistoryItem> Trace_GetHistoryByTraceNr(long TraceNr)
        {
            List<TraceNrHistoryItem> TraceNrHistory = new List<TraceNrHistoryItem>();
            try
            {
                IEnumerable<IDataRecord> readerlist = FillDataReaderBySQL("EXECUTE GetHistoryByTNr " + TraceNr, sb_DefaultDBConnectionString.ConnectionString);
                foreach (var reader in readerlist)
                {
                    TraceNrHistory.Add(new TraceNrHistoryItem
                    {
                        JobID = reader["JobID"] == DBNull.Value ? "" : ((int)reader["JobID"]).ToString(),
                        StartTime = reader["StartTime"] == DBNull.Value ? null : (DateTime?)reader["StartTime"],
                        Action = reader["Action"] == DBNull.Value ? "" : (string)reader["Action"],
                        JobResult = reader["JobResult"] == DBNull.Value ? "" : (string)reader["JobResult"],
                        ErrorCode = reader["ErrorCode"] == DBNull.Value ? "" : (string)reader["ErrorCode"],
                        ErrorDetails = reader["ErrorDetails"] == DBNull.Value ? "" : (string)reader["ErrorDetails"],
                        //Rank1 = (int)reader["Rank1"],
                        //Rank2 = (int)reader["Rank2"],
                    });
                }
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Error in Trace_GetHistoryByTraceNr: " + ex.ToString();
                return null;
            }
            return TraceNrHistory;
        }
        public List<TraceNrHistoryItem> Trace_GetHistoryByRFIDTag(string TagSerialNr)
        {
            List<TraceNrHistoryItem> TraceNrHistory = new List<TraceNrHistoryItem>();
            try
            {
                IEnumerable<IDataRecord> readerlist = FillDataReaderBySQL("EXECUTE GetHistoryByRFIDTag " + TagSerialNr, sb_DefaultDBConnectionString.ConnectionString);
                foreach (var reader in readerlist)
                {
                    TraceNrHistory.Add(new TraceNrHistoryItem
                    {
                        JobID = reader["JobID"] == DBNull.Value ? "" : ((int)reader["JobID"]).ToString(),
                        StartTime = reader["StartTime"] == DBNull.Value ? null : (DateTime?)reader["StartTime"],
                        Action = reader["Action"] == DBNull.Value ? "" : (string)reader["Action"],
                        JobResult = reader["JobResult"] == DBNull.Value ? "" : (string)reader["JobResult"],
                        ErrorCode = reader["ErrorCode"] == DBNull.Value ? "" : (string)reader["ErrorCode"],
                        ErrorDetails = reader["ErrorDetails"] == DBNull.Value ? "" : (string)reader["ErrorDetails"],
                        //Rank1 = (int)reader["Rank1"],
                        //Rank2 = (int)reader["Rank2"],
                    });
                }
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Error in Trace_GetHistoryByRFIDTag: " + ex.ToString();
                return null;
            }
            return TraceNrHistory;
        }
        #endregion

        #region Methods - Calibration
        public bool Calibration_LoadCalibers()
        {
            Calibers = new List<Caliber>();
            try
            {
                IEnumerable<IDataRecord> readerlist = FillDataReaderBySQL("EXEC Trace_GetCalibers", sb_DefaultDBConnectionString.ConnectionString);
                foreach (var reader in readerlist)
                {
                    Calibers.Add(new Caliber
                    {
                        ID_DefaultValue = (int)reader["ID_DefaultValue"],
                        IDWS = (int)reader["ID_WS"],
                        SubWS = (int)reader["SubWS"],
                        ID_SubWS = (int)reader["ID_SubWS"],
                        TraceNr = (long)reader["TraceNr"],
                    });
                }
                CaliberDefaultValue = -1;
                return true;
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Error in Calibration_LoadCalibers: " + ex.ToString();
                return false;
            }
        }
        public bool Calibration_CheckCaliber(long TraceNr, int IDWS, int SubWS)
        {
            Caliber myCaliber = Calibers.Where(x => x.TraceNr == TraceNr && x.IDWS == IDWS && x.SubWS == SubWS).FirstOrDefault();
            if (myCaliber == null)
            {
                return false;
            }
            CaliberDefaultValue = myCaliber.ID_DefaultValue;
            return true;
        }
        public bool Calibration_CheckCaliber(long TraceNr, int IDWS, int SubWS, out bool IsCaliber, out bool IsCaliberForThisWS)
        {
            IsCaliber = false;
            IsCaliberForThisWS = false;

            Caliber myCaliber = Calibers.Where(x => x.TraceNr == TraceNr).FirstOrDefault();

            if (myCaliber != null)
            {
                IsCaliber = true;
                IsCaliberForThisWS = (myCaliber.IDWS == IDWS && myCaliber.SubWS == SubWS);
            }
            return true;
        }
        public bool Calibration_SaveMeasurement(long TraceNr, long IDWS, long SubWS, int ID_DefaultValue, double MeasuredValue, double CalibrationDevice_MeasurementValue, double CalibrationError, string Description)
        {
            SqlCommand cmd = new SqlCommand();
            cmd.CommandText = "sp_Calibration_SaveMeasurement";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@TraceNr", TraceNr);
            cmd.Parameters["@TraceNr"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@WSID", IDWS);
            cmd.Parameters["@WSID"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@SubWS", SubWS);
            cmd.Parameters["@SubWS"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@ID_DefaultValue", ID_DefaultValue);
            cmd.Parameters["@ID_DefaultValue"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@EOLMeasuredValue", MeasuredValue);
            cmd.Parameters["@EOLMeasuredValue"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@CalibrationDevice_MeasurementValue", CalibrationDevice_MeasurementValue);
            cmd.Parameters["@CalibrationDevice_MeasurementValue"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@CalibrationError", CalibrationError);
            cmd.Parameters["@CalibrationError"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@Description", Description);
            cmd.Parameters["@Description"].Direction = ParameterDirection.Input;

            try
            {
                return DBExecuteNonQuery(ref cmd);
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Calibration_SaveMeasurement: " + ex.ToString();
                return false;
            }
        }
        public List<CalibrationMeasurement> Calibration_GetMeasurementList(long TraceNr, int IDWS, int SubWS)
        {
            List<CalibrationMeasurement> CalibrationMeasurements = new List<CalibrationMeasurement>();
            try
            {
                IEnumerable<IDataRecord> readerlist = FillDataReaderBySQL("EXEC sp_Calibration_GetMeasurementList " + TraceNr + ", " + IDWS + ", " + SubWS, sb_DefaultDBConnectionString.ConnectionString);
                foreach (var reader in readerlist)
                {
                    CalibrationMeasurements.Add(new CalibrationMeasurement
                    {
                        ID_DefaultValue = (int)reader["ID_DefaultValue"],
                        Name = (string)reader["Name"],
                        NominalValue = (float)reader["NominalValue"],
                        Tolerance = (float)reader["Tolerance"],
                        Units = (string)reader["Units"],
                        CalibrationError = (float)reader["CalibrationError"],
                    });
                }
                CaliberDefaultValue = -1;
                return CalibrationMeasurements;
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Error in Calibration_GetMeasurementList: " + ex.ToString();
                return null;
            }
        }
        //public CalibrationAdjustment Calibration_GetLastAdjustment(string MeasurementName, int IDWS, int SubWS)
        //  {
        //  CalibrationAdjustment LastAdjustment = new CalibrationAdjustment();
        //  try {
        //    IEnumerable<IDataRecord> readerlist = FillDataReaderBySQL("EXEC sp_Calibration_GetLastAdjustment '" + TinyRef + "', '" + MeasurementName + "', " + IDWS + ", " + SubWS, sb_DefaultDBConnectionString.ConnectionString);
        //    foreach (var reader in readerlist) {
        //      LastAdjustment = new CalibrationAdjustment
        //        {
        //        CalibrationDate = (DateTime)reader["Date"],
        //        Name = (string)reader["Name"],
        //        Value = (float)reader["EOL_MeasuredValue"],
        //        OffSet = (float)reader["OffSet"],
        //        };
        //      }
        //    }
        //  catch (Exception ex) {
        //    LastErrorDescription = "Error in Calibration_GetLastAdjustment: " + ex.ToString();
        //    return null;
        //    }
        //  return LastAdjustment;
        //  }
        public List<CalibrationAdjustment> Calibration_GetLastAdjustments(int IDWS, int SubWS)
        {
            List<CalibrationAdjustment> LastAdjustments = new List<CalibrationAdjustment>();
            try
            {
                IEnumerable<IDataRecord> readerlist = FillDataReaderBySQL("EXEC sp_Calibration_GetLastAdjustments " + IDWS + ", " + SubWS, sb_DefaultDBConnectionString.ConnectionString);
                foreach (var reader in readerlist)
                {
                    LastAdjustments.Add(new CalibrationAdjustment
                    {
                        CalibrationDate = (DateTime)reader["Date"],
                        Name = (string)reader["Name"],
                        Value = (float)reader["EOL_MeasuredValue"],
                        OffSet = (float)reader["OffSet"],
                    });
                }
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Error in Calibration_GetLastAdjustment: " + ex.ToString();
                return null;
            }
            return LastAdjustments;
        }
        #endregion

        #region Methods - Packaging
        private int Package_CheckPart(long PackageTraceNr, long PartTraceNr, int ID_Ref, int PackageNetWeight, string ControlVersion)
        {
            SqlCommand cmd = new SqlCommand();
            cmd.CommandText = "Package_CheckPart";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@PackageTraceNr", PackageTraceNr);
            cmd.Parameters["@PackageTraceNr"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@PartTraceNr", PartTraceNr);
            cmd.Parameters["@PartTraceNr"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@ID_Ref", ID_Ref);
            cmd.Parameters["@ID_Ref"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@PackageNetWeight", PackageNetWeight);
            cmd.Parameters["@PackageNetWeight"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@ControlVersion", ControlVersion);
            cmd.Parameters["@ControlVersion"].Direction = ParameterDirection.Input;

            cmd.Parameters.Add("@Return", SqlDbType.Int);
            cmd.Parameters["@Return"].Direction = ParameterDirection.ReturnValue;

            try
            {
                return DBExecuteNonQuery(ref cmd) ? (int)cmd.Parameters["@Return"].Value : -1;
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Package_CheckPart: " + ex.ToString();
                return -1;
            }
        }
        private int Package_Close(long PackageTraceNr, out int PartsInPackage)
        {
            PartsInPackage = 0;
            SqlCommand cmd = new SqlCommand();
            cmd.CommandText = "Package_Close";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@PackageTraceNr", PackageTraceNr);
            cmd.Parameters["@PackageTraceNr"].Direction = ParameterDirection.Input;

            cmd.Parameters.Add("@PartsInPackage", SqlDbType.SmallInt);
            cmd.Parameters["@PartsInPackage"].Direction = ParameterDirection.Output;

            cmd.Parameters.Add("@Return", SqlDbType.Int);
            cmd.Parameters["@Return"].Direction = ParameterDirection.ReturnValue;

            try
            {
                if (!DBExecuteNonQuery(ref cmd)) return -1;
                PartsInPackage = (int)cmd.Parameters["@PartsInPackage"].Value;
                return (int)cmd.Parameters["@Return"].Value;
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Package_Close: " + ex.ToString();
                return -1;
            }
        }
        private int Package_CountParts(long PackageTraceNr, out int PartsInPackage)
        {
            PartsInPackage = 0;
            SqlCommand cmd = new SqlCommand();
            cmd.CommandText = "Package_CountParts";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@PackageTraceNr", PackageTraceNr);
            cmd.Parameters["@PackageTraceNr"].Direction = ParameterDirection.Input;

            cmd.Parameters.Add("@PartsInPackage", SqlDbType.SmallInt);
            cmd.Parameters["@PartsInPackage"].Direction = ParameterDirection.Output;

            cmd.Parameters.Add("@Return", SqlDbType.Int);
            cmd.Parameters["@Return"].Direction = ParameterDirection.ReturnValue;

            try
            {
                if (!DBExecuteNonQuery(ref cmd)) return -1;
                PartsInPackage = (int)cmd.Parameters["@PartsInPackage"].Value;
                return (int)cmd.Parameters["@Return"].Value;
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Package_CountParts: " + ex.ToString();
                return -1;
            }
        }
        private int Package_InsertObject(long PackageTraceNr, int ID_PackagingObject, int PackageNewNetWeight, int ID_PackagingWeighingMachine)
        {
            SqlCommand cmd = new SqlCommand();
            cmd.CommandText = "Package_InsertObject";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@PackageTraceNr", PackageTraceNr);
            cmd.Parameters["@PackageTraceNr"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@ID_PackagingObject", ID_PackagingObject);
            cmd.Parameters["@ID_PackagingObject"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@PackageNewNetWeight", PackageNewNetWeight);
            cmd.Parameters["@PackageNewNetWeight"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@ID_PackagingWeighingMachine", ID_PackagingWeighingMachine);
            cmd.Parameters["@ID_PackagingWeighingMachine"].Direction = ParameterDirection.Input;

            cmd.Parameters.Add("@Return", SqlDbType.Int);
            cmd.Parameters["@Return"].Direction = ParameterDirection.ReturnValue;

            try
            {
                return DBExecuteNonQuery(ref cmd) ? (int)cmd.Parameters["@Return"].Value : -1;
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Package_InsertObject: " + ex.ToString();
                return -1;
            }
        }
        private int Package_InsertPart(long PackageTraceNr, long PartTraceNr, int ID_Ref, string ControlVersion, int PackageNewNetWeight, int ID_PackagingWeighingMachine, out int PartsInPackage, out bool PackageFull)
        {
            PartsInPackage = 0;
            PackageFull = false;
            SqlCommand cmd = new SqlCommand();
            cmd.CommandText = "Package_InsertPart";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@PackageTraceNr", PackageTraceNr);
            cmd.Parameters["@PackageTraceNr"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@PartTraceNr", PartTraceNr);
            cmd.Parameters["@PartTraceNr"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@ID_Ref", ID_Ref);
            cmd.Parameters["@ID_Ref"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@ControlVersion", ControlVersion);
            cmd.Parameters["@ControlVersion"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@PackageNewNetWeight", PackageNewNetWeight);
            cmd.Parameters["@PackageNewNetWeight"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@ID_PackagingWeighingMachine", ID_PackagingWeighingMachine);
            cmd.Parameters["@ID_PackagingWeighingMachine"].Direction = ParameterDirection.Input;

            cmd.Parameters.Add("@PartsInPackage", SqlDbType.Int);
            cmd.Parameters["@PartsInPackage"].Direction = ParameterDirection.Output;

            cmd.Parameters.Add("@PackageFull", SqlDbType.Int);
            cmd.Parameters["@PackageFull"].Direction = ParameterDirection.Output;

            cmd.Parameters.Add("@Return", SqlDbType.Int);
            cmd.Parameters["@Return"].Direction = ParameterDirection.ReturnValue;

            try
            {
                if (!DBExecuteNonQuery(ref cmd)) return -1;
                PartsInPackage = (int)cmd.Parameters["@PartsInPackage"].Value;
                PackageFull = (bool)cmd.Parameters["@PackageFull"].Value;
                return (int)cmd.Parameters["@Return"].Value;
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Package_InsertPart: " + ex.ToString();
                return -1;
            }
        }
        private int Package_OpenNew(long PackageTraceNr, int ID_PackagingProfile, int ID_Ref, string ControlVersion, int Tare)
        {
            SqlCommand cmd = new SqlCommand();
            cmd.CommandText = "Package_OpenNew";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@PackageTraceNr", PackageTraceNr);
            cmd.Parameters["@PackageTraceNr"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@ID_PackagingProfile", ID_PackagingProfile);
            cmd.Parameters["@ID_PackagingProfile"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@Tare", Tare);
            cmd.Parameters["@Tare"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@ID_Ref", ID_Ref);
            cmd.Parameters["@ID_Ref"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@ControlVersion", ControlVersion);
            cmd.Parameters["@ControlVersion"].Direction = ParameterDirection.Input;

            cmd.Parameters.Add("@Return", SqlDbType.Int);
            cmd.Parameters["@Return"].Direction = ParameterDirection.ReturnValue;

            try
            {
                return DBExecuteNonQuery(ref cmd) ? (int)cmd.Parameters["@Return"].Value : -1;
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Package_OpenNew: " + ex.ToString();
                return -1;
            }
        }
        private int Package_RemoveObject(long PackageTraceNr, int ID_PackagingObject, int PackageNewNetWeight, int ID_PackagingWeighingMachine)
        {
            SqlCommand cmd = new SqlCommand();
            cmd.CommandText = "Package_RemoveObject";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@PackageTraceNr", PackageTraceNr);
            cmd.Parameters["@PackageTraceNr"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@ID_PackagingObject", ID_PackagingObject);
            cmd.Parameters["@ID_PackagingObject"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@PackageNewNetWeight", PackageNewNetWeight);
            cmd.Parameters["@PackageNewNetWeight"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@ID_PackagingWeighingMachine", ID_PackagingWeighingMachine);
            cmd.Parameters["@ID_PackagingWeighingMachine"].Direction = ParameterDirection.Input;

            cmd.Parameters.Add("@Return", SqlDbType.Int);
            cmd.Parameters["@Return"].Direction = ParameterDirection.ReturnValue;

            try
            {
                return DBExecuteNonQuery(ref cmd) ? (int)cmd.Parameters["@Return"].Value : -1;
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Package_RemoveObject: " + ex.ToString();
                return -1;
            }
        }
        private int Package_RemovePart(long PackageTraceNr, long PartTraceNr)
        {
            SqlCommand cmd = new SqlCommand();
            cmd.CommandText = "Package_RemovePart";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@PackageTraceNr", PackageTraceNr);
            cmd.Parameters["@PackageTraceNr"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@PartTraceNr", PartTraceNr);
            cmd.Parameters["@PartTraceNr"].Direction = ParameterDirection.Input;

            cmd.Parameters.Add("@Return", SqlDbType.Int);
            cmd.Parameters["@Return"].Direction = ParameterDirection.ReturnValue;

            try
            {
                return DBExecuteNonQuery(ref cmd) ? (int)cmd.Parameters["@Return"].Value : -1;
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Package_RemovePart: " + ex.ToString();
                return -1;
            }
        }
        private int Package_ReOpen(long PackageTraceNr)
        {
            SqlCommand cmd = new SqlCommand();
            cmd.CommandText = "Package_ReOpen";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@PackageTraceNr", PackageTraceNr);
            cmd.Parameters["@PackageTraceNr"].Direction = ParameterDirection.Input;

            cmd.Parameters.Add("@Return", SqlDbType.Int);
            cmd.Parameters["@Return"].Direction = ParameterDirection.ReturnValue;

            try
            {
                return DBExecuteNonQuery(ref cmd) ? (int)cmd.Parameters["@Return"].Value : -1;
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Package_ReOpen: " + ex.ToString();
                return -1;
            }
        }
        private int Package_ReplacePart(long PackageTraceNr, long OldPartTraceNr, long NewPartTraceNr)
        {
            SqlCommand cmd = new SqlCommand();
            cmd.CommandText = "Package_ReplacePart";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@PackageTraceNr", PackageTraceNr);
            cmd.Parameters["@PackageTraceNr"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@OldPartTraceNr", OldPartTraceNr);
            cmd.Parameters["@OldPartTraceNr"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@NewPartTraceNr", NewPartTraceNr);
            cmd.Parameters["@NewPartTraceNr"].Direction = ParameterDirection.Input;

            cmd.Parameters.Add("@Return", SqlDbType.Int);
            cmd.Parameters["@Return"].Direction = ParameterDirection.ReturnValue;

            try
            {
                return DBExecuteNonQuery(ref cmd) ? (int)cmd.Parameters["@Return"].Value : -1;
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Package_RemovePart: " + ex.ToString();
                return -1;
            }
        }
        private int Package_TransferPart(long NewPackageTraceNr, long PartTraceNr, string ControlVersion, int PackageNewNetWeight, int ID_PackagingWeighingMachine, out int PartsInPackage, out bool PackageFull)
        {
            PartsInPackage = 0;
            PackageFull = false;
            SqlCommand cmd = new SqlCommand();
            cmd.CommandText = "Package_TransferPart";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@NewPackageTraceNr", NewPackageTraceNr);
            cmd.Parameters["@PackageTraceNr"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@PartTraceNr", PartTraceNr);
            cmd.Parameters["@PartTraceNr"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@ControlVersion", ControlVersion);
            cmd.Parameters["@ControlVersion"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@PackageNewNetWeight", PackageNewNetWeight);
            cmd.Parameters["@PackageNewNetWeight"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@ID_PackagingWeighingMachine", ID_PackagingWeighingMachine);
            cmd.Parameters["@ID_PackagingWeighingMachine"].Direction = ParameterDirection.Input;

            cmd.Parameters.Add("@PartsInPackage", SqlDbType.Int);
            cmd.Parameters["@PartsInPackage"].Direction = ParameterDirection.Output;

            cmd.Parameters.Add("@PackageFull", SqlDbType.Int);
            cmd.Parameters["@PackageFull"].Direction = ParameterDirection.Output;

            cmd.Parameters.Add("@Return", SqlDbType.Int);
            cmd.Parameters["@Return"].Direction = ParameterDirection.ReturnValue;

            try
            {
                if (!DBExecuteNonQuery(ref cmd)) return -1;
                PartsInPackage = (int)cmd.Parameters["@PartsInPackage"].Value;
                PackageFull = (bool)cmd.Parameters["@PackageFull"].Value;
                return (int)cmd.Parameters["@Return"].Value;
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Package_TransferPart: " + ex.ToString();
                return -1;
            }
        }
        private int Package_UpdateTare(long PackageTraceNr, int Tare)
        {
            SqlCommand cmd = new SqlCommand();
            cmd.CommandText = "Package_UpdateTare";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@PackageTraceNr", PackageTraceNr);
            cmd.Parameters["@PackageTraceNr"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@Tare", Tare);
            cmd.Parameters["@Tare"].Direction = ParameterDirection.Input;

            cmd.Parameters.Add("@Return", SqlDbType.Int);
            cmd.Parameters["@Return"].Direction = ParameterDirection.ReturnValue;

            try
            {
                return DBExecuteNonQuery(ref cmd) ? (int)cmd.Parameters["@Return"].Value : -1;
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Package_RemovePart: " + ex.ToString();
                return -1;
            }
        }

        #endregion

        #region Methods - Email
        /// <summary>
        /// 
        /// </summary>
        /// <param name="Subject">The Email subject.</param>
        /// <param name="BodyMessage">The message to send</param>
        /// <param name="From">Only the sender name. A valid mail address will be added automatically</param>
        /// <param name="Recipients">The Recipient mail addressses separated by ';'</param>
        /// <param name="CopyRecipients">CC Recipient mail addressses separated by ';'</param>
        /// <param name="BlindCopyRecipients">BCC Recipient Mail addressses separated by ';'</param>
        /// <param name="SQLMailProfile">Optional. The default SQL Mail profile value is defined in the [Trace_SendEmail] stored procedure.</param>
        /// <returns></returns>
        public int Email_SendEmail(string Subject, string BodyMessage, string From, string Recipients, string CopyRecipients, string BlindCopyRecipients, string SQLMailProfile = "")
        {
            SqlCommand cmd = new SqlCommand("Trace_SendEmail");
            cmd.CommandType = CommandType.StoredProcedure;

            if (!SQLMailProfile.Trim().Equals(""))
            {
                cmd.Parameters.AddWithValue("@MailProfile", SQLMailProfile);
                cmd.Parameters["@MailProfile"].Direction = ParameterDirection.Input;
            }

            cmd.Parameters.AddWithValue("@MailSubject", Subject);
            cmd.Parameters["@MailSubject"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@BodyMessage", BodyMessage);
            cmd.Parameters["@BodyMessage"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@mailrecipients", Recipients);
            cmd.Parameters["@mailrecipients"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@mailcopyrecipients", CopyRecipients);
            cmd.Parameters["@mailcopyrecipients"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@mailblindcopyrecipients", BlindCopyRecipients);
            cmd.Parameters["@mailblindcopyrecipients"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@mailfrom", From);
            cmd.Parameters["@mailfrom"].Direction = ParameterDirection.Input;

            cmd.Parameters.Add("@Return", SqlDbType.Int);
            cmd.Parameters["@Return"].Direction = ParameterDirection.ReturnValue;

            try
            {
                return DBExecuteNonQuery(ref cmd) ? (int)cmd.Parameters["@Return"].Value : -1;
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Trace_SendEmail: " + ex.ToString();
                return -1;
            }
        }
        #endregion

        #region Methods - Device Results
        private int Result_GetResultCharacteristicID(int ID_DeviceResultSubCategory, string Characteristic)
        {
            try
            {
                if (ResultCharacteristics.Where(x => x.CharacteristicName.ToLower().Trim().Replace(" ", "").Equals(Characteristic.ToLower().Trim().Replace(" ", ""))).Count() == 0)
                {
                    Result_InsertCharacteristic(ID_DeviceResultSubCategory, Characteristic);
                    Result_LoadResultCharacteristics();
                }
                ResultCharacteristic c = ResultCharacteristics.Where(x => x.CharacteristicName.ToLower().Trim().Replace(" ", "").Equals(Characteristic.ToLower().Trim().Replace(" ", ""))).First();
                return c == null ? -1 : c.ID_DeviceResultTestCharacteristic;
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Result_GetResultCharacteristicID: " + ex.ToString();
                return -1;
            }
        }
        private int Result_GetResultObjectID(string Object)
        {
            try
            {
                if (ResultObjects.Where(x => x.TestObjectName.ToLower().Trim().Replace(" ", "").Equals(Object.ToLower().Trim().Replace(" ", ""))).Count() == 0)
                {
                    Result_InsertObject(Object);
                    Result_LoadResultObjects();
                }
                ResultObject o = ResultObjects.Where(x => x.TestObjectName.ToLower().Trim().Replace(" ", "").Equals(Object.ToLower().Trim().Replace(" ", ""))).First();
                return o == null ? -1 : o.ID_DeviceResultTestObject;
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Result_GetResultObjectID: " + ex.ToString();
                return -1;
            }
        }
        private bool ValidateResultSubCategories()
        {
            //Loads Enum to List:
            var EnumList = new List<KeyValuePair<string, int>>();
            foreach (var e in Enum.GetValues(typeof(ResultSubCategory)))
            {
                EnumList.Add(new KeyValuePair<string, int>(e.ToString(), (int)e));
            }

            //If items in database are less than the items on Enum (missing items on database):
            if (EnumList.Count > ResultSubCategories.Count) return false;

            //Checks if the SubCategory name and SubCategory ID matches on both lists:
            foreach (var EnumItem in EnumList)
            {
                if (ResultSubCategories.Where(x => x.ID_DeviceResultSubCategory == EnumItem.Value && x.SubCategoryDescription.ToLower().Equals(EnumItem.Key.ToLower())).Count() != 1)
                {
                    return false;
                }
            }
            return true;
        }
        public int Result_InsertCharacteristic(int ID_DeviceResultSubCategory, string CharacteristicName)
        {
            SqlCommand cmd = new SqlCommand();
            cmd.CommandText = "DeviceResultInsertCharacteristic";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@ID_DeviceResultSubCategory", ID_DeviceResultSubCategory);
            cmd.Parameters["@ID_DeviceResultSubCategory"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@CharacteristicName", CharacteristicName);
            cmd.Parameters["@CharacteristicName"].Direction = ParameterDirection.Input;

            cmd.Parameters.Add("@Return", SqlDbType.Int);
            cmd.Parameters["@Return"].Direction = ParameterDirection.ReturnValue;

            try
            {
                return DBExecuteNonQuery(ref cmd) ? (int)cmd.Parameters["@Return"].Value : -1;
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Result_InsertCharacteristic: " + ex.ToString();
                return -1;
            }
        }
        public int Result_InsertObject(string ObjectName)
        {
            SqlCommand cmd = new SqlCommand();
            cmd.CommandText = "DeviceResultInsertObject";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@TestObjectName", ObjectName);
            cmd.Parameters["@TestObjectName"].Direction = ParameterDirection.Input;

            cmd.Parameters.Add("@Return", SqlDbType.Int);
            cmd.Parameters["@Return"].Direction = ParameterDirection.ReturnValue;

            try
            {
                return DBExecuteNonQuery(ref cmd) ? (int)cmd.Parameters["@Return"].Value : -1;
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Result_InsertObject: " + ex.ToString();
                return -1;
            }
        }
        public bool Result_LoadResultCharacteristics()
        {
            ResultCharacteristics = new List<ResultCharacteristic>();
            try
            {
                IEnumerable<IDataRecord> readerlist = FillDataReaderBySQL("SELECT * FROM [vwDeviceResultCharacteristics]", sb_DefaultDBConnectionString.ConnectionString);
                foreach (var reader in readerlist)
                {
                    ResultCharacteristics.Add(new ResultCharacteristic
                    {
                        ID_DeviceResultTestCharacteristic = (int)reader["ID_DeviceResultTestCharacteristic"],
                        ID_DeviceResultCategory = (int)reader["ID_DeviceResultCategory"],
                        ID_DeviceResultSubCategory = (int)reader["ID_DeviceResultSubCategory"],
                        CharacteristicName = (string)reader["CharacteristicName"],
                    });
                }
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Result_LoadResultCharacteristics: " + ex.ToString();
                return false;
            }
            return true;
        }
        public bool Result_LoadResultObjects()
        {
            ResultObjects = new List<ResultObject>();
            try
            {
                IEnumerable<IDataRecord> readerlist = FillDataReaderBySQL("SELECT * FROM [vwDeviceResultObjects]", sb_DefaultDBConnectionString.ConnectionString);
                foreach (var reader in readerlist)
                {
                    ResultObjects.Add(new ResultObject
                    {
                        ID_DeviceResultTestObject = (int)reader["ID_DeviceResultTestObject"],
                        TestObjectName = (string)reader["TestObjectName"],
                    });
                }
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Result_LoadResultObjects: " + ex.ToString();
                return false;
            }
            return true;
        }
        public bool Result_LoadResultSubCategories()
        {
            ResultSubCategories = new List<ResultSubCategoryInDB>();
            try
            {

                String Query = "SELECT SC.[ID_DeviceResultSubCategory] " +
                               "      ,SC.[ID_DeviceResultCategory] " +
                               "      ,REPLACE(C.[CategoryName] + '_' + SC.[SubCategoryName], ' ', '') AS 'SubCategoryDescription' " +
                               "FROM  [DeviceResultSubCategories] SC INNER JOIN " +
                               "      [DeviceResultCategories] C ON SC.ID_DeviceResultCategory = C.ID_DeviceResultCategory";


                IEnumerable<IDataRecord> readerlist = FillDataReaderBySQL(Query, sb_DefaultDBConnectionString.ConnectionString);
                foreach (var reader in readerlist)
                {
                    ResultSubCategories.Add(new ResultSubCategoryInDB
                    {
                        ID_DeviceResultSubCategory = (int)reader["ID_DeviceResultSubCategory"],
                        ID_DeviceResultCategory = (int)reader["ID_DeviceResultCategory"],
                        SubCategoryDescription = (string)reader["SubCategoryDescription"],
                    });
                }
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Result_LoadResultObjects: " + ex.ToString();
                return false;
            }
            return true;
        }

        private void AddToResultErrorsList(int ID_WSJob, ResultSubCategory SubCategory, int ID_DeviceResultTestCharacteristic, int ID_DeviceResultTestObject)
        {
            if (TestErrorResults == null) return;
            TestErrorResults.Add(new TestErrorResult
            {
                ID_WSJob = ID_WSJob,
                ID_DeviceResultCategory = ResultCharacteristics.Where(x => x.ID_DeviceResultTestCharacteristic == ID_DeviceResultTestCharacteristic).FirstOrDefault().ID_DeviceResultCategory,
                ID_DeviceResultSubCategory = ResultCharacteristics.Where(x => x.ID_DeviceResultTestCharacteristic == ID_DeviceResultTestCharacteristic).FirstOrDefault().ID_DeviceResultSubCategory,
                ID_DeviceResultTestCharacteristic = ID_DeviceResultTestCharacteristic,
                ID_DeviceResultTestObject = ID_DeviceResultTestObject,
                SubCategoryName = SubCategory.ToString().Replace(" ", ""),
                CharacteristicName = ResultCharacteristics.Where(x => x.ID_DeviceResultTestCharacteristic == ID_DeviceResultTestCharacteristic).FirstOrDefault().CharacteristicName,
                TestObjectName = ResultObjects.Where(x => x.ID_DeviceResultTestObject == ID_DeviceResultTestObject).FirstOrDefault().TestObjectName,
            });
        }
        public bool Result_SaveResult(int ID_WSJob, string Value, string Expected, string Characteristic, string Object, ResultSubCategory SubCategory, ResultUnit Unit, string Description)
        {
            int ID_DeviceResultTestCharacteristic = Result_GetResultCharacteristicID((int)SubCategory, Characteristic);
            int ID_DeviceResultTestObject = Result_GetResultObjectID(Object);
            if (ID_DeviceResultTestCharacteristic <= 0 || ID_DeviceResultTestObject <= 0) return false;

            //If value Nok, add to Errors list
            if (!Value.Equals(Expected)) AddToResultErrorsList(ID_WSJob, SubCategory, ID_DeviceResultTestCharacteristic, ID_DeviceResultTestObject);

            SqlCommand cmd = new SqlCommand();
            cmd.CommandText = "DeviceSaveStringResult";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@ID_WSJob", ID_WSJob);
            cmd.Parameters["@ID_WSJob"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@ResultDescription", Description);
            cmd.Parameters["@ResultDescription"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@TextValue", Value);
            cmd.Parameters["@TextValue"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@TextValueExpected", Expected);
            cmd.Parameters["@TextValueExpected"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@ID_DeviceResultTestCharacteristic", ID_DeviceResultTestCharacteristic);
            cmd.Parameters["@ID_DeviceResultTestCharacteristic"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@ID_DeviceResultTestObject", ID_DeviceResultTestObject);
            cmd.Parameters["@ID_DeviceResultTestObject"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@ID_DeviceResultUnit", (int)Unit);
            cmd.Parameters["@ID_DeviceResultUnit"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@ResultOK", Value.Equals(Expected));
            cmd.Parameters["@ResultOK"].Direction = ParameterDirection.Input;

            cmd.Parameters.Add("@Return", SqlDbType.Int);
            cmd.Parameters["@Return"].Direction = ParameterDirection.ReturnValue;

            try
            {
                return DBExecuteNonQuery(ref cmd) ? (int)cmd.Parameters["@Return"].Value == 0 : false;
            }
            catch (Exception ex)
            {
                LastErrorDescription = "DeviceSaveStringResult: " + ex.ToString();
                return false;
            }
        }
        public bool Result_SaveResult(int ID_WSJob, bool Value, bool Expected, string Characteristic, string Object, ResultSubCategory SubCategory, ResultUnit Unit, string Description)
        {
            int ID_DeviceResultTestCharacteristic = Result_GetResultCharacteristicID((int)SubCategory, Characteristic);
            int ID_DeviceResultTestObject = Result_GetResultObjectID(Object);
            if (ID_DeviceResultTestCharacteristic <= 0 || ID_DeviceResultTestObject <= 0) return false;

            //If value Nok, add to Errors list
            if (Value != Expected) AddToResultErrorsList(ID_WSJob, SubCategory, ID_DeviceResultTestCharacteristic, ID_DeviceResultTestObject);

            SqlCommand cmd = new SqlCommand();
            cmd.CommandText = "DeviceSaveBooleanResult";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@ID_WSJob", ID_WSJob);
            cmd.Parameters["@ID_WSJob"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@ResultDescription", Description);
            cmd.Parameters["@ResultDescription"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@BoolValue", Value);
            cmd.Parameters["@BoolValue"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@BoolExpected", Expected);
            cmd.Parameters["@BoolExpected"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@ID_DeviceResultTestCharacteristic", ID_DeviceResultTestCharacteristic);
            cmd.Parameters["@ID_DeviceResultTestCharacteristic"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@ID_DeviceResultTestObject", ID_DeviceResultTestObject);
            cmd.Parameters["@ID_DeviceResultTestObject"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@ID_DeviceResultUnit", (int)Unit);
            cmd.Parameters["@ID_DeviceResultUnit"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@ResultOK", (Value == Expected));
            cmd.Parameters["@ResultOK"].Direction = ParameterDirection.Input;

            cmd.Parameters.Add("@Return", SqlDbType.Int);
            cmd.Parameters["@Return"].Direction = ParameterDirection.ReturnValue;

            try
            {
                return DBExecuteNonQuery(ref cmd) ? (int)cmd.Parameters["@Return"].Value == 0 : false;
            }
            catch (Exception ex)
            {
                LastErrorDescription = "DeviceSaveBooleanResult: " + ex.ToString();
                return false;
            }
        }
        public bool Result_SaveResult(int ID_WSJob, double Value, double Min, double Max, string Characteristic, string Object, ResultSubCategory SubCategory, ResultUnit Unit, string Description)
        {
            int ID_DeviceResultTestCharacteristic = Result_GetResultCharacteristicID((int)SubCategory, Characteristic);
            int ID_DeviceResultTestObject = Result_GetResultObjectID(Object);
            if (ID_DeviceResultTestCharacteristic <= 0 || ID_DeviceResultTestObject <= 0) return false;

            //If value Nok, add to Errors list
            if (Value < Min || Value > Max) AddToResultErrorsList(ID_WSJob, SubCategory, ID_DeviceResultTestCharacteristic, ID_DeviceResultTestObject);

            SqlCommand cmd = new SqlCommand();
            cmd.CommandText = "DeviceSaveNumericResult";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@ID_WSJob", ID_WSJob);
            cmd.Parameters["@ID_WSJob"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@ResultDescription", Description);
            cmd.Parameters["@ResultDescription"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@Value", Value);
            cmd.Parameters["@Value"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@MinVal", Min);
            cmd.Parameters["@MinVal"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@MaxVal", Max);
            cmd.Parameters["@MaxVal"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@ID_DeviceResultTestCharacteristic", ID_DeviceResultTestCharacteristic);
            cmd.Parameters["@ID_DeviceResultTestCharacteristic"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@ID_DeviceResultTestObject", ID_DeviceResultTestObject);
            cmd.Parameters["@ID_DeviceResultTestObject"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@ID_DeviceResultUnit", (int)Unit);
            cmd.Parameters["@ID_DeviceResultUnit"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@ResultOK", (Value >= Min && Value <= Max));
            cmd.Parameters["@ResultOK"].Direction = ParameterDirection.Input;

            cmd.Parameters.Add("@Return", SqlDbType.Int);
            cmd.Parameters["@Return"].Direction = ParameterDirection.ReturnValue;

            try
            {
                return DBExecuteNonQuery(ref cmd) ? (int)cmd.Parameters["@Return"].Value == 0 : false;
            }
            catch (Exception ex)
            {
                LastErrorDescription = "DeviceSaveNumericResult: " + ex.ToString();
                return false;
            }
        }
        #endregion

        #region Methods - Device Constants
        public bool Const_LoadConstants(int ID_SubWS)
        {
            DeviceConsts = new List<DeviceConst>();
            SqlCommand cmd = new SqlCommand("GetConsts");

            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@IDSubWS", ID_SubWS);
            cmd.Parameters["@IDSubWS"].Direction = ParameterDirection.Input;

            cmd.Parameters.Add("@Return", SqlDbType.Int);
            cmd.Parameters["@Return"].Direction = ParameterDirection.ReturnValue;

            try
            {
                IEnumerable<IDataRecord> readerlist = FillDataReaderBySQL(cmd, sb_DefaultDBConnectionString.ConnectionString);
                if (readerlist.Count() > 0)
                {

                    foreach (var reader in readerlist)
                    {
                        DeviceConsts.Add(new DeviceConst
                        {
                            ID_Const = (int)reader["ID_Const"],
                            ID_SubWS = (int)reader["ID_SubWS"],
                            ConstValue = reader["ConstValue"] == DBNull.Value ? "" : (string)reader["ConstValue"],
                            ConstName = reader["ConstName"] == DBNull.Value ? "" : (string)reader["ConstName"],
                            ConstDescription = reader["ConstDescription"] == DBNull.Value ? "" : (string)reader["ConstDescription"],
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Error in Const_GetConstants: " + ex.ToString();
                return false;
            }
            return true;
        }
        public string Const_GetTextValue(string ConstName)
        {
            if (DeviceConsts.Where(x => x.ConstName.ToLower().Trim() == ConstName.ToLower().Trim()).Count() == 0) return "";
            return DeviceConsts.Where(x => x.ConstName.ToLower().Trim() == ConstName.ToLower().Trim()).FirstOrDefault().ConstValue;
        }
        /// <exception cref="ConstantNotFoundException"></exception>
        public double Const_GetNumericValue(string ConstName)
        {
            double result = 0;
            if (DeviceConsts.Where(x => x.ConstName.ToLower().Trim() == ConstName.ToLower().Trim()).Count() == 0)
                throw new ConstantNotFoundException("Constant name not found");

            if (!double.TryParse(DeviceConsts.Where(x => x.ConstName.ToLower().Trim() == ConstName.ToLower().Trim()).FirstOrDefault().ConstValue, out result))
                throw new ConstantNotFoundException("The Constant was found, but it is not convertible to a numeric value");

            return result;
        }
        /// <exception cref="ConstantNotFoundException"></exception>
        public bool Const_GetBooleanValue(string ConstName)
        {
            short auxresult = -1;

            if (DeviceConsts.Where(x => x.ConstName.ToLower().Trim() == ConstName.ToLower().Trim()).Count() == 0)
                throw new ConstantNotFoundException("Constant name not found");

            if (!Int16.TryParse(DeviceConsts.Where(x => x.ConstName.ToLower().Trim() == ConstName.ToLower().Trim()).FirstOrDefault().ConstValue, out auxresult))
                throw new ConstantNotFoundException("The Constant was found, but can not be converted to a Boolean value");

            return Convert.ToBoolean(auxresult);
        }
        /// <summary>
        /// Updates a MeasureLimit and saves changes in the Log.
        /// </summary>
        public bool Const_UpdateConstant(int ID_SubWS, int ID_User, string ConstantValue, string ConstantName)
        {
            SqlCommand cmd = new SqlCommand("UpdateDeviceConstant");
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@ConstValue", ConstantValue);
            cmd.Parameters["@ConstValue"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@ConstName", ConstantName);
            cmd.Parameters["@ConstName"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@ID_SubWS", ID_SubWS);
            cmd.Parameters["@ID_SubWS"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@ID_User", ID_User);
            cmd.Parameters["@ID_User"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@SaveChangeToLog", (ID_User >= 0));
            cmd.Parameters["@SaveChangeToLog"].Direction = ParameterDirection.Input;

            cmd.Parameters.Add("@Return", SqlDbType.Int);
            cmd.Parameters["@Return"].Direction = ParameterDirection.ReturnValue;

            try
            {
                if (!DBExecuteNonQuery(ref cmd)) return false;
                return (int)cmd.Parameters["@Return"].Value == 0;
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Error in Const_UpdateConstant: " + ex.ToString();
                return false;
            }
        }
        #endregion

        #region Methods - EOL/SerialNumbers
        public int SerialNr_CreateEOLSerialNr(long JobID, out string ProductionDate, out string DayCounter, out string EOLSerialNr)
        {
            ProductionDate = "";
            DayCounter = "";
            EOLSerialNr = "";

            SqlCommand cmd = new SqlCommand();
            cmd.CommandText = "EOL_CreateSerialNr";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@JobID", JobID);
            cmd.Parameters["@JobID"].Direction = ParameterDirection.Input;

            cmd.Parameters.Add("@EOLSNr", SqlDbType.BigInt);
            cmd.Parameters["@EOLSNr"].Direction = ParameterDirection.Output;

            cmd.Parameters.Add("@ProdDate", SqlDbType.DateTime);
            cmd.Parameters["@ProdDate"].Direction = ParameterDirection.Output;

            cmd.Parameters.Add("@DayCounter", SqlDbType.SmallInt);
            cmd.Parameters["@DayCounter"].Direction = ParameterDirection.Output;

            cmd.Parameters.Add("@Return", SqlDbType.Int);
            cmd.Parameters["@Return"].Direction = ParameterDirection.ReturnValue;

            try
            {
                if (!DBExecuteNonQuery(ref cmd)) return -1;
                ProductionDate = String.Format("{0:dd/MM/yy}", (DateTime)cmd.Parameters["@ProdDate"].Value).Replace("-", ".");
                DayCounter = ((Int16)cmd.Parameters["@DayCounter"].Value).ToString("0000");
                EOLSerialNr = ((long)cmd.Parameters["@EOLSNr"].Value).ToString("0000");
                return (int)cmd.Parameters["@Return"].Value;
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Error in SerialNr_CreateEOLSerialNr: " + ex.ToString();
                return -1;
            }
        }
        public int SerialNr_GetEOLSerialNr(long TraceNr, out string ProductionDate, out string DayCounter, out string EOLSerialNr, out string CustomerSerialNr)
        {
            ProductionDate = "";
            DayCounter = "";
            EOLSerialNr = "";
            CustomerSerialNr = "";

            SqlCommand cmd = new SqlCommand();
            cmd.CommandText = "EOL_GetEOLSerialNr";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@TNr", TraceNr);
            cmd.Parameters["@TNr"].Direction = ParameterDirection.Input;

            cmd.Parameters.Add("@EOLSNr", SqlDbType.BigInt);
            cmd.Parameters["@EOLSNr"].Direction = ParameterDirection.Output;

            cmd.Parameters.Add("@ProdDate", SqlDbType.DateTime);
            cmd.Parameters["@ProdDate"].Direction = ParameterDirection.Output;

            cmd.Parameters.Add("@DayCounter", SqlDbType.SmallInt);
            cmd.Parameters["@DayCounter"].Direction = ParameterDirection.Output;

            cmd.Parameters.Add("@CustomerSerialNr", SqlDbType.NVarChar, 150);
            cmd.Parameters["@CustomerSerialNr"].Direction = ParameterDirection.Output;

            cmd.Parameters.Add("@Return", SqlDbType.Int);
            cmd.Parameters["@Return"].Direction = ParameterDirection.ReturnValue;

            try
            {
                if (!DBExecuteNonQuery(ref cmd)) return -1;
                ProductionDate = String.Format("{0:dd/MM/yy}", (DateTime)cmd.Parameters["@ProdDate"].Value).Replace("-", ".");
                DayCounter = ((Int16)cmd.Parameters["@DayCounter"].Value).ToString("0000");
                EOLSerialNr = ((long)cmd.Parameters["@EOLSNr"].Value).ToString("0000");
                CustomerSerialNr = (cmd.Parameters["@CustomerSerialNr"].Value).ToString();
                return (int)cmd.Parameters["@Return"].Value;
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Error in SerialNr_GetEOLSerialNr: " + ex.ToString();
                return -1;
            }
        }
        public int SerialNr_SaveCustomerSerialNr(long JobID, string CustomerSerialNr)
        {
            SqlCommand cmd = new SqlCommand();
            cmd.CommandText = "EOL_SaveCustomerSerialNr";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@JobID", JobID);
            cmd.Parameters["@JobID"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@CustomerSerialNr", CustomerSerialNr);
            cmd.Parameters["@CustomerSerialNr"].Direction = ParameterDirection.Input;

            cmd.Parameters.Add("@Return", SqlDbType.Int);
            cmd.Parameters["@Return"].Direction = ParameterDirection.ReturnValue;

            try
            {
                return DBExecuteNonQuery(ref cmd) ? (int)cmd.Parameters["@Return"].Value : -1;
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Error in SerialNr_SaveCustomerSerialNr: " + ex.ToString();
                return -1;
            }
        }
        #endregion

        #region Methods - Samples
        public bool Samples_LoadSamples()
        {
            Samples = new List<Sample>();
            try
            {
                IEnumerable<IDataRecord> readerlist = FillDataReaderBySQL("EXEC Trace_GetSamples", sb_DefaultDBConnectionString.ConnectionString);

                foreach (var reader in readerlist)
                {
                    Samples.Add(new Sample
                    {
                        ID_Sample = (int)reader["ID_Sample"],
                        IDWS = (int)reader["ID_WS"],
                        SubWS = (int)reader["SubWS"],
                        ID_SubWS = (int)reader["ID_SubWS"],
                        SampleDetails = reader["SampleDetails"] == DBNull.Value ? "" : (string)reader["SampleDetails"],
                        TraceNr = (long)reader["TraceNr"],
                        OkSample = ((string)reader["SampleStatus"]).Trim().ToLower().Equals("ok"),
                        ErrorCode = (((string)reader["SampleStatus"]).Trim().ToLower().Equals("ok") || reader["ErrorCode"] == DBNull.Value) ? "" : (string)reader["ErrorCode"],
                    });
                }
                //TODO
                //if (Samples.Where(x => x.OkSample == true).Count() == 0 && Samples.Where(x => x.OkSample == false).Count() == 0) {
                //  Samples = null;
                //  return false;
                //  }

                return true;
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Error in Samples_LoadSamples: " + ex.ToString();
                return false;
            }
        }
        public bool Samples_CheckSample(long TraceNr, int IDWS, int SubWS)
        {
            //ErrorCode = "";
            Sample mySample = Samples.Where(x => x.TraceNr == TraceNr && x.IDWS == IDWS && x.SubWS == SubWS).FirstOrDefault();
            if (mySample == null)
            {
                return false;
            }
            //ErrorCode = mySample.ErrorCode;
            return true;
        }
        public bool Samples_CheckSample(long TraceNr, int IDWS, int SubWS, out Sample sample)
        {
            sample = Samples.Where(x => x.TraceNr == TraceNr).FirstOrDefault();
            if (sample == null) return false;

            if (sample.IDWS == IDWS && sample.SubWS == SubWS) sample.IsSampleForThisWS = true;
            return true;
        }
        public bool Samples_CheckSample(long TraceNr, int IDSubWS)
        {
            //ErrorCode = "";
            Sample mySample = Samples.Where(x => x.TraceNr == TraceNr && x.ID_SubWS == IDSubWS).FirstOrDefault();
            if (mySample == null)
            {
                return false;
            }
            //ErrorCode = mySample.ErrorCode;
            return true;
        }
        public bool Samples_CheckSample(long TraceNr, int IDSubWS, out Sample sample)
        {
            sample = Samples.Where(x => x.TraceNr == TraceNr).FirstOrDefault();
            if (sample == null) return false;

            if (sample.ID_SubWS == IDSubWS) sample.IsSampleForThisWS = true;
            return true;
        }
        public bool Samples_ValidateSampleTesting(long TraceNr, int IDSubWS)
        {
            Sample mySample = Samples.Where(x => x.TraceNr == TraceNr && x.ID_SubWS == IDSubWS).FirstOrDefault();
            if (mySample == null) return false;

            //Ok Sample:
            if (mySample.OkSample == true)
            {
                if (/*Samples.Count() != TestErrorResults.Count() ||*/ TestErrorResults.Count() > 0) return false;
            }
            //Nok Sample:
            else
            {
                foreach (var item in Samples.Where(x => x.TraceNr == TraceNr && x.ID_SubWS == IDSubWS))
                {
                    if (!TestErrorResults.Exists(x => x.ErrorCode == item.ErrorCode)) return false;
                }
            }
            return true;
        }
        #endregion

        #region Methods - Claim Parts
        public bool Claim_InsertClaimPart(long TraceNr)
        {
            SqlCommand cmd = new SqlCommand();
            cmd.CommandText = "InsertClaimPart";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@TraceNr", TraceNr);
            cmd.Parameters["@TraceNr"].Direction = ParameterDirection.Input;

            cmd.Parameters.Add("@Return", SqlDbType.Int);
            cmd.Parameters["@Return"].Direction = ParameterDirection.ReturnValue;

            try
            {
                return DBExecuteNonQuery(ref cmd);
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Error in Claim_InsertClaimPart: " + ex.ToString();
                return false;
            }
        }
        public bool Claim_CheckClaimPart(long TraceNr, out bool IsClaimPart)
        {
            IsClaimPart = false;

            SqlCommand cmd = new SqlCommand();
            cmd.CommandText = "CheckClaimPart";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@TraceNr", TraceNr);
            cmd.Parameters["@TraceNr"].Direction = ParameterDirection.Input;

            cmd.Parameters.Add("@Status", SqlDbType.SmallInt);
            cmd.Parameters["@Status"].Direction = ParameterDirection.Output;

            cmd.Parameters.Add("@Return", SqlDbType.Int);
            cmd.Parameters["@Return"].Direction = ParameterDirection.ReturnValue;

            try
            {
                if (!DBExecuteNonQuery(ref cmd)) return false;
                IsClaimPart = (((Int16)cmd.Parameters["@Status"].Value) == 1);
                return true;
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Error in Claim_CheckClaimPart: " + ex.ToString();
                return false;
            }
        }
        public bool Claim_UpdateClaimPart(long TraceNr)
        {
            SqlCommand cmd = new SqlCommand();
            cmd.CommandText = "UpdateClaimPart";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@TraceNr", TraceNr);
            cmd.Parameters["@TraceNr"].Direction = ParameterDirection.Input;

            cmd.Parameters.Add("@Return", SqlDbType.Int);
            cmd.Parameters["@Return"].Direction = ParameterDirection.ReturnValue;

            try
            {
                return DBExecuteNonQuery(ref cmd);
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Error in Claim_UpdateClaimPart: " + ex.ToString();
                return false;
            }
        }
        #endregion

        #region Methods - SyncDate with SQLServer
        public struct SystemTime
        {
            public ushort Year;
            public ushort Month;
            public ushort DayOfWeek;
            public ushort Day;
            public ushort Hour;
            public ushort Minute;
            public ushort Second;
            public ushort Millisecond;
        };
        public SystemTime mySystemTime;

        [System.Runtime.InteropServices.DllImport("kernel32.dll", EntryPoint = "GetSystemTime", SetLastError = true)]
        public extern static void Win32GetSystemTime(ref SystemTime sysTime);
        [System.Runtime.InteropServices.DllImport("kernel32.dll", EntryPoint = "SetSystemTime", SetLastError = true)]
        public extern static bool Win32SetSystemTime(ref SystemTime sysTime);
        public bool GetDate(out DateTime ServerDateTime)
        {
            ServerDateTime = DateTime.MinValue;
            try
            {
                ServerDateTime = (DateTime)DBExecuteScalar(new SqlCommand("SELECT GETDATE()"));
                return true;
            }
            catch (Exception ex)
            {
                LastErrorDescription = "GetDate: " + ex.ToString();
                return false;
            }
        }
        public bool SyncDate()
        {
            try
            {
                DateTime ServerDate;

                if (!GetDate(out ServerDate)) return false;

                mySystemTime.Year = (ushort)ServerDate.Year;
                mySystemTime.Month = (ushort)ServerDate.Month;
                mySystemTime.Day = (ushort)ServerDate.Day;
                mySystemTime.Hour = (ushort)ServerDate.Hour;
                mySystemTime.Minute = (ushort)ServerDate.Minute;
                mySystemTime.Second = (ushort)ServerDate.Second;

                return Win32SetSystemTime(ref mySystemTime);
            }
            catch
            {
                return false;
            }
        }
        #endregion

        #region Methods - Laser Tasks/Reworks
        public List<LaserTask> Laser_LoadTasks(long TraceNr)
        {
            List<LaserTask> Tasks = new List<LaserTask>();
            try
            {
                IEnumerable<IDataRecord> readerlist = FillDataReaderBySQL("EXEC [Laser].[GetLaserTasks] " + TraceNr, sb_DefaultDBConnectionString.ConnectionString);

                foreach (var reader in readerlist)
                {
                    Tasks.Add(new LaserTask
                    {
                        LaserProfile = (string)reader["LaserProfile"],
                        JOBName = (string)reader["JOBName"],
                        BINName = (string)reader["BINName"],
                        CompleteJob = (bool)reader["CompleteJob"],
                        FromRework = (bool)reader["FromRework"],
                    });
                }
                //ErrorCode = "";
                return Tasks;
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Error in Laser_LoadTasks: " + ex.ToString();
                return null;
            }
        }
        public List<LaserTask> Laser_LoadProfileData(int IDRef)
        {
            List<LaserTask> Tasks = new List<LaserTask>();
            try
            {
                IEnumerable<IDataRecord> readerlist = FillDataReaderBySQL("EXEC [Laser].[GetLaserProfiles] " + IDRef, sb_DefaultDBConnectionString.ConnectionString);

                foreach (var reader in readerlist)
                {
                    Tasks.Add(new LaserTask
                    {
                        LaserProfile = (string)reader["LaserProfile"],
                        JOBName = (string)reader["JOBName"],
                        BINName = (string)reader["BINName"],
                        BINDescription = (string)reader["BINDescription"],
                        CompleteJob = false,
                        FromRework = false,
                    });
                }
                return Tasks;
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Error in Laser_LoadProfileData: " + ex.ToString();
                return null;
            }
        }
        public bool Laser_DeleteLaserRework(long TraceNr, int id_LaserProfile)
        {
            SqlCommand cmd = new SqlCommand("Laser.DeleteLaserRework");
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@id_LaserProfile", id_LaserProfile);
            cmd.Parameters["@id_LaserProfile"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@TraceNr", TraceNr);
            cmd.Parameters["@TraceNr"].Direction = ParameterDirection.Input;

            cmd.Parameters.Add("@Return", SqlDbType.Int);
            cmd.Parameters["@Return"].Direction = ParameterDirection.ReturnValue;

            try
            {
                return DBExecuteNonQuery(ref cmd) ? (int)cmd.Parameters["@Return"].Value == 0 : false;
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Laser_DeleteLaserRework: " + ex.ToString();
                return false;
            }
        }
        public bool Laser_UpdateLaserRework(long TraceNr, int id_LaserProfile, bool TaskDone)
        {
            SqlCommand cmd = new SqlCommand("Laser.UpdateLaserRework");
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@id_LaserProfile", id_LaserProfile);
            cmd.Parameters["@id_LaserProfile"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@TaskDone", TaskDone);
            cmd.Parameters["@TaskDone"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@TraceNr", TraceNr);
            cmd.Parameters["@TraceNr"].Direction = ParameterDirection.Input;

            cmd.Parameters.Add("@Return", SqlDbType.Int);
            cmd.Parameters["@Return"].Direction = ParameterDirection.ReturnValue;

            try
            {
                return DBExecuteNonQuery(ref cmd) ? (int)cmd.Parameters["@Return"].Value == 0 : false;
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Laser_UpdateLaserRework: " + ex.ToString();
                return false;
            }
        }
        public bool Laser_InsertLaserRework(long TraceNr, int id_LaserProfile, int id_ReworkJob)
        {
            SqlCommand cmd = new SqlCommand("Laser.InsertLaserRework");
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@id_ReworkJob", id_ReworkJob);
            cmd.Parameters["@id_ReworkJob"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@id_LaserProfile", id_LaserProfile);
            cmd.Parameters["@id_LaserProfile"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@TraceNr", TraceNr);
            cmd.Parameters["@TraceNr"].Direction = ParameterDirection.Input;

            cmd.Parameters.Add("@Return", SqlDbType.Int);
            cmd.Parameters["@Return"].Direction = ParameterDirection.ReturnValue;

            try
            {
                return DBExecuteNonQuery(ref cmd) ? (int)cmd.Parameters["@Return"].Value == 0 : false;
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Laser_InsertLaserRework: " + ex.ToString();
                return false;
            }
        }
        #endregion

        #region Methods - System Configurations
        public bool SysConfig_LoadSystemConfigurations(int IDRef, int ID_SubWS)
        {
            SystemConfigurations = new List<SystemConfiguration>();
            try
            {

                IEnumerable<IDataRecord> readerlist = FillDataReaderBySQL(string.Format("EXECUTE GetSystemConfigurations {0}, {1}", IDRef, ID_SubWS), sb_DefaultDBConnectionString.ConnectionString);
                foreach (var reader in readerlist)
                {

                    SystemConfigurations.Add(new SystemConfiguration
                    {
                        ID_Ref = (int)reader["ID_Ref"],
                        ID_SystemConfiguration = (int)reader["ID_SystemConfiguration"],
                        ConfigurationProfile = (string)reader["ConfigurationProfile"],
                        ID_SubWS = (int)reader["ID_SubWS"],
                        ID_WS = (int)reader["ID_WS"],
                        SubWS = (int)reader["SubWS"],
                        ConfigurationName = (string)reader["ConfigurationName"],
                        Value = (string)reader["Value"],
                        Unit = (string)reader["Unit"],
                        Active = (bool)reader["Active"],
                        Description = (string)reader["Description"],
                        Variant = (string)reader["Variant"],
                        ProfileType = (string)reader["ProfileType"]
                    });
                }
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Error in Measures_LoadMeasureLimits: " + ex.ToString();
                return false;
            }
            return true;
        }
        public bool SysConfig_LoadGenericSystemConfigurations(int ID_SubWS)
        {
            return SysConfig_LoadSystemConfigurations(ID_SubWS, 0);
        }
        public string SysConfig_GetTextValue(string ConfigurationName)
        {
            if (SystemConfigurations.Where(x => x.ConfigurationName.ToLower().Trim() == ConfigurationName.ToLower().Trim()).Count() == 0) return "";
            return SystemConfigurations.Where(x => x.ConfigurationName.ToLower().Trim() == ConfigurationName.ToLower().Trim()).FirstOrDefault().Value.Trim();
        }
        /// <exception cref="ConfigurationNotFoundException"></exception>
        public double SysConfig_GetNumericValue(string ConfigurationName)
        {
            double result = 0;
            if (SystemConfigurations.Where(x => x.ConfigurationName.ToLower().Trim() == ConfigurationName.ToLower().Trim()).Count() == 0)
                throw new ConfigurationNotFoundException("Configuration name not found");

            if (!double.TryParse(SystemConfigurations.Where(x => x.ConfigurationName.ToLower().Trim() == ConfigurationName.ToLower().Trim()).FirstOrDefault().Value.Trim(), out result))
                throw new ConfigurationNotFoundException("The Configuration name was found, but it is not convertible to a numeric value");

            return result;
        }
        /// <exception cref="ConfigurationNotFoundException"></exception>
        public bool SysConfig_GetBooleanValue(string ConfigurationName)
        {
            short auxresult = -1;

            if (SystemConfigurations.Where(x => x.ConfigurationName.ToLower().Trim() == ConfigurationName.ToLower().Trim()).Count() == 0)
                throw new ConfigurationNotFoundException("Configuration name not found");

            if (!Int16.TryParse(SystemConfigurations.Where(x => x.ConfigurationName.ToLower().Trim() == ConfigurationName.ToLower().Trim()).FirstOrDefault().Value.Trim(), out auxresult))
                throw new ConfigurationNotFoundException("The Configuration name was found, but can not be converted to a Boolean value");

            return Convert.ToBoolean(auxresult);
        }
        #endregion

        #region Methods - MeasureLimits
        public bool Measures_LoadMeasureLimits(int IDRef, int ID_SubWS)
        {
            MeasureLimits = new List<MeasureLimit>();
            try
            {

                IEnumerable<IDataRecord> readerlist = FillDataReaderBySQL(string.Format("EXECUTE GetMeasureLimits {0}, {1}", IDRef, ID_SubWS), sb_DefaultDBConnectionString.ConnectionString);
                foreach (var reader in readerlist)
                {

                    MeasureLimits.Add(new MeasureLimit
                    {
                        ID_Ref = (int)reader["ID_Ref"],
                        ID_MeasureLimit = (int)reader["ID_MeasureLimit"],
                        MeasureProfile = (string)reader["MeasureProfile"],
                        ID_SubWS = (int)reader["ID_SubWS"],
                        ID_WS = (int)reader["ID_WS"],
                        SubWS = (int)reader["SubWS"],
                        MeasureName = (string)reader["MeasureName"],
                        MinValue = (float)reader["MinValue"],
                        MaxValue = (float)reader["MaxValue"],
                        Unit = (string)reader["Unit"],
                        Active = (bool)reader["Active"],
                        Description = (string)reader["Description"],
                        Variant = (string)reader["Variant"],
                        ProfileType = (string)reader["ProfileType"]
                    });
                }
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Error in Measures_LoadMeasureLimits: " + ex.ToString();
                return false;
            }
            return true;
        }
        public bool Measures_LoadGenericMeasureLimits(int ID_SubWS)
        {
            return Measures_LoadMeasureLimits(ID_SubWS, 0);
        }
        public bool Measures_GetLimits(string MeasureName, ref double MinValue, ref double MaxValue, ref bool Enabled, ref string Unit, ref string Description)
        {
            MeasureLimit myLimit = MeasureLimits.Where(x => x.MeasureName.ToLower().Equals(MeasureName.ToLower())).FirstOrDefault();
            if (myLimit == null) return false;

            MinValue = myLimit.MinValue;
            MaxValue = myLimit.MaxValue;
            Enabled = myLimit.Active;
            Unit = myLimit.Unit;
            Description = myLimit.Description;
            return true;
        }
        public MeasureLimit Measures_GetLimits(string MeasureName)
        {
            return MeasureLimits.Where(x => x.MeasureName.ToLower().Equals(MeasureName.ToLower())).FirstOrDefault();
        }
        public bool Measures_CheckNumericLimits(string MeasureName, double Value)
        {
            MeasureLimit myLimit = MeasureLimits.Where(x => x.MeasureName.ToLower().Equals(MeasureName.ToLower())).FirstOrDefault();
            if (myLimit == null) throw new MeasureLimitNotFoundException();

            return (Value >= myLimit.MinValue && Value <= myLimit.MaxValue);
        }
        public bool Measures_CheckBooleanLimits(string MeasureName, bool Value)
        {
            MeasureLimit myLimit = MeasureLimits.Where(x => x.MeasureName.ToLower().Equals(MeasureName.ToLower())).FirstOrDefault();

            if (myLimit == null) throw new MeasureLimitNotFoundException();
            if (myLimit.MinValue != myLimit.MaxValue) throw new MeasureLimitNotFoundException("MinValue and MaxValue have different values. Could not determine a Boolean state");
            //if (!Convert.ToBoolean(myLimit.MinValue) || !Convert.ToBoolean(myLimit.MaxValue)) throw new MeasureLimitNotFoundException("MinValue or MaxValue can not be converted to a Boolean type");

            return (Convert.ToBoolean(myLimit.MinValue) == Value);
        }
        /// <summary>
        /// Updates a MeasureLimit and saves the change to a change log.
        /// </summary>
        public bool Measures_UpdateMeasureLimit(int ID_User, int ID_Ref, int ID_SubWS, int ID_MeasureLimit, double MinValue, double MaxValue, string Unit, string Variant, bool Active, string Description)
        {
            bool SaveChangeToLog = (ID_User > 0 && ID_Ref > 0);

            SqlCommand cmd = new SqlCommand("UpdateMeasureLimit");
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@ID_User", ID_User);
            cmd.Parameters["@ID_User"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@ID_Ref", ID_Ref);
            cmd.Parameters["@ID_Ref"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@ID_SubWS", ID_SubWS);
            cmd.Parameters["@ID_SubWS"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@ID_MeasureLimit", ID_MeasureLimit);
            cmd.Parameters["@ID_MeasureLimit"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@MinValue", MinValue);
            cmd.Parameters["@MinValue"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@MaxValue", MaxValue);
            cmd.Parameters["@MaxValue"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@Unit", Unit);
            cmd.Parameters["@Unit"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@Variant", Variant);
            cmd.Parameters["@Variant"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@Active", Active);
            cmd.Parameters["@Active"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@Description", Description);
            cmd.Parameters["@Description"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@SaveChangeToLog", SaveChangeToLog);
            cmd.Parameters["@SaveChangeToLog"].Direction = ParameterDirection.Input;

            cmd.Parameters.Add("@Return", SqlDbType.Int);
            cmd.Parameters["@Return"].Direction = ParameterDirection.ReturnValue;

            try
            {
                if (!DBExecuteNonQuery(ref cmd)) return false;
                return (int)cmd.Parameters["@Return"].Value == 0;
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Error in Measures_UpdateMeasureLimit: " + ex.ToString();
                return false;
            }
        }
        /// <summary>
        /// Updates a MeasureLimit without saving changes in the Log.
        /// </summary>
        public bool Measures_UpdateMeasureLimit(int ID_SubWS, int ID_MeasureLimit, double MinValue, double MaxValue, string Unit, string Variant, bool Active, string Description)
        {
            return Measures_UpdateMeasureLimit(-1, -1, ID_SubWS, ID_MeasureLimit, MinValue, MaxValue, Unit, Variant, Active, Description);
        }
        #endregion
    }


    /// <summary>
    /// All methods. Inherits PPTraceDB and PPBaseDB.
    /// </summary>
    /// <exception cref="StationNotFoundException">The supplied IDWS and SubWS are invalid</exception>
    public class PPTraceStation : PPTraceDB
    {
        #region Vars
        protected Reference myPrehReference;
        protected bool myStatus = false;
        protected int myIDWSC = 0;
        protected int myIDWS = 0;
        protected int mySubWS = 0;
        protected int myIDSubWS = 0;
        protected int myIDJob = 0;
        //protected int myIDRef = 0;
        //protected string myRefPreh = "";
        protected string myWSName = "";
        protected string mySubWSName = "";
        protected string myWSCName = "";
        protected bool myWSIsRepeatable = false;
        #endregion

        #region Properties
        public Reference SelectedReference
        {
            get { return myPrehReference; }
        }
        //public string RefPreh
        //  {
        //  get { return myRefPreh; }
        //  }
        //public int RefID
        //  {
        //  get { return myIDRef; }
        //  }
        public int JobID
        {
            get { return myIDJob; }
        }
        public int IDWorkstation
        {
            get { return myIDWS; }
        }
        public int IDWSCenter
        {
            get { return myIDWSC; }
        }
        public int SubWS
        {
            get { return mySubWS; }
        }
        public int IDSubWS
        {
            get { return myIDSubWS; }
        }
        public string WorkstationName
        {
            get { return myWSName; }
        }
        public string SubWSName
        {
            get { return mySubWSName; }
        }
        public string WSCenterName
        {
            get { return myWSCName; }
        }
        #endregion

        #region Constructors / Private
        /// <summary>
        /// 
        /// </summary>
        /// <param name="DefaultConnString">The default connection string for this instance</param>
        /// <param name="ProdGenericsConnString">The ProdGenerics/ProdGeral database connection string for this instance</param>
        /// <param name="IDWS">The Station identifier: IDWorkstation</param>
        /// <param name="SubWS">The Station identifier: SubWorkstation</param>
        /// <param name="LoadDataOnStartUp">Loads Devices results, Samples, Calibrations, Constants and Messages data on startup</param>
        public PPTraceStation(SqlConnectionStringBuilder DefaultConnString, SqlConnectionStringBuilder ProdGenericsConnString, int IDWS, int SubWS, bool LoadDataOnStartUp = true)
          : base(DefaultConnString, ProdGenericsConnString, 10, LoadDataOnStartUp)
        {
            LoadStationData(IDWS, SubWS);
            if (myIDSubWS <= 0) throw new StationNotFoundException();

            if (LoadDataOnStartUp)
            {
                Calibration_LoadCalibers();
                Languages_LoadMessages();
                Const_LoadConstants();
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="DefaultConnString">The default connection string for this instance</param>
        /// <param name="ProdGenericsConnString">The ProdGenerics/ProdGeral database connection string for this instance</param>
        /// <param name="IDWS">The Station identifier: IDWorkstation</param>
        /// <param name="SubWS">The Station identifier: SubWorkstation</param>
        /// <param name="LoadDataOnStartUp">Loads Devices results, Samples, Calibrations, Constants and Messages data on startup</param>
        public PPTraceStation(SqlConnectionStringBuilder DefaultConnString, SqlConnectionStringBuilder ProdGenericsConnString, int IDWS, int SubWS, string UserGroup, bool LoadDataOnStartUp = true)
          : base(DefaultConnString, ProdGenericsConnString, 5, LoadDataOnStartUp)
        {
            LoadStationData(IDWS, SubWS);
            if (myIDSubWS <= 0) throw new StationNotFoundException();

            if (LoadDataOnStartUp)
            {
                Samples_LoadSamples();
                Calibration_LoadCalibers();
                Languages_LoadMessages();
                Const_LoadConstants();
                Users_GetAppActiveUsers(UserGroup.Trim());
            }
        }
        private bool LoadStationData(int IDWS, int SubWS)
        {
            myIDSubWS = 0;
            myWSName = "";
            mySubWSName = "";
            myIDWSC = 0;
            myWSCName = "";
            try
            {
                IEnumerable<IDataRecord> readerlist = FillDataReaderBySQL("EXECUTE Trace_GetSubwsDataFromIDWSAndSubWS " + IDWS + "," + SubWS, sb_DefaultDBConnectionString.ConnectionString);

                foreach (var reader in readerlist)
                {
                    myWSName = (string)reader["WSName"];
                    mySubWSName = (string)reader["SubWSName"];
                    myIDWSC = (int)reader["ID_WSCenter"];
                    myWSCName = (string)reader["WSCName"];
                    myIDSubWS = (int)reader["ID_SubWS"];
                    myIDWS = IDWS;
                    mySubWS = SubWS;
                }
                return true;
            }
            catch (Exception ex)
            {
                LastErrorDescription = "Error in LoadStationData: " + ex.ToString();
                return false;
            }
        }
        #endregion

        #region Station Methods - Device Settings (Consts)
        public bool Const_LoadConstants()
        {
            return Const_LoadConstants(myIDSubWS);
        }
        /// <summary>
        /// Updates a Constant and saves changes in the Log.
        /// </summary>
        public bool Const_UpdateConstant(int IDUser, string ConstValue, string ConstName)
        {
            return Const_UpdateConstant(myIDSubWS, IDUser, ConstValue, ConstName);
        }
        /// <summary>
        /// Updates a MeasureLimit without saving changes in the Log.
        /// </summary>
        public bool Const_UpdateConstant(string ConstValue, string ConstName)
        {
            return Const_UpdateConstant(myIDSubWS, -1, ConstValue, ConstName);
        }
        #endregion

        #region Methods - EOL/SerialNumbers
        public int SerialNr_CreateEOLSerialNr(out string ProductionDate, out string DayCounter, out string EOLSerialNr)
        {
            return SerialNr_CreateEOLSerialNr(myIDJob, out ProductionDate, out DayCounter, out EOLSerialNr);
        }
        public int SerialNr_SaveCustomerSerialNr(string CustomerSerialNr)
        {
            return SerialNr_SaveCustomerSerialNr(myIDJob, CustomerSerialNr);
        }
        #endregion

        #region Station Methods - Languages
        public bool Languages_LoadMessages()
        {
            return base.Languages_LoadMessages(myIDSubWS);
        }
        public string Languages_GetMessage(string message)
        {
            return base.Languages_GetMessage(message, myIDSubWS);
        }
        public string Languages_GetGenericMessage(string message)
        {
            return base.Languages_GetMessage(message, 0);
        }
        //private bool Languages_InsertNewMsg(string message)
        //  {
        //  return Languages_InsertNewMsg(message, myIDSubWS);
        //  }
        public bool Languages_UpdateMsg(string OldMessage, string NewMessage)
        {
            return Languages_UpdateMsg(OldMessage, NewMessage, myIDSubWS);
        }
        public bool Languages_UpdateGenericMsg(string OldMessage, string NewMessage)
        {
            return Languages_UpdateMsg(OldMessage, NewMessage, 0);
        }

        #endregion

        #region Station Methods - Traceability
        public bool SetReference(long TraceNr)
        {
            return SetReference(Trace_GetIDFromTraceNr(TraceNr));
        }
        public bool SetReference(string RefPreh)
        {
            return SetReference(Trace_GetIDRefFromRefPreh(RefPreh));
        }
        public bool SetReference(int IDRef)
        {
            myPrehReference = Trace_GetRefDataByIDRef(IDRef);
            if (myPrehReference == null) return false;

            if (myPrehReference.IDRef <= 0)
            {
                myPrehReference = null;
                return false;
            }
            else
            {
                return true; // Measures_LoadMeasureLimits();
            }
            //myIDRef = Ref.IDRef > 0 ? Ref.IDRef : 0;
            //myRefPreh = Ref.IDRef > 0 ? Ref.RefPreh : "";
            //if (myIDRef > 0) Measures_LoadMeasureLimits();

            //return (myIDRef > 0);
        }

        public List<TraceNrHistoryItem> Trace_GetHistoryByTraceNr(long TraceNr, out bool IsSample)
        {
            IsSample = Samples_CheckSample(TraceNr, myIDWS, mySubWS);
            return Trace_GetHistoryByTraceNr(TraceNr);
        }
        public int Trace_AssignRef(long TraceNr, string RefPreh)
        {
            return Trace_AssignRef(TraceNr, RefPreh, myIDWS, mySubWS);
        }
        public int Trace_CheckAssembly(long TraceNrParent, long TraceNrChild)
        {
            return Trace_CheckAssembly(TraceNrParent, TraceNrChild, myIDWS, mySubWS);
        }
        public int Trace_CheckAssemblyByRefPreh(string RefParent, string RefChild)
        {
            return Trace_CheckAssemblyByRefPreh(RefParent, RefChild, myIDWS, mySubWS);
        }
        public int Trace_CheckTraceNr(long TraceNr, string RefPreh)
        {
            return Trace_CheckTraceNr(TraceNr, RefPreh, myIDWS, mySubWS);
        }
        public int Trace_SaveAssembly(long TraceNrParent, long TraceNrChild)
        {
            return Trace_SaveAssembly(TraceNrParent, TraceNrChild, myIDJob);
        }
        public int Trace_CheckUpgradeRef(long TraceNr, string FinalRef)
        {
            return Trace_CheckUpgradeRef(TraceNr, FinalRef, myIDWS, mySubWS);
        }
        public int Trace_UpgradeRef(long TraceNr, string FinalRef)
        {
            return Trace_UpgradeRef(TraceNr, FinalRef, myIDWS, mySubWS, myIDJob);
        }
        public int Trace_JobStart(long TraceNr)
        {
            return Trace_JobStart(TraceNr, myIDWS, mySubWS, ref myIDJob);
        }
        public int Trace_JobEnd(byte JobResult)
        {
            int result = Trace_JobEnd(myIDJob, JobResult);
            if (result == 0) myIDJob = 0;
            return result;
        }
        public int Trace_JobEnd(byte JobResult, int ErrorCode, string ErrorDetails)
        {
            int result = Trace_JobEndWithErrorDetails(myIDJob, JobResult, ErrorCode, ErrorDetails);
            if (result == 0) myIDJob = 0;
            return result;
        }
        public int Trace_JobSaveError(int ErrorCode, string ErrorDetails)
        {
            return Trace_JobSaveError(myIDJob, ErrorCode, ErrorDetails);
        }
        public int Trace_GetIDSubWSFromIDWSAndSubWS()
        {
            return Trace_GetIDSubWSFromIDWSAndSubWS(myIDWS, mySubWS);
        }
        #endregion

        #region Station Methods - Critical Errors
        public List<PartErrorConfiguration> Critical_GetErrorConfiguration(long TraceNr)
        {
            return Critical_GetErrorConfiguration(myIDSubWS, TraceNr);
        }
        //public bool DeviceErrorGetErrorConfigurationByPart(int ID_SubWS, long TraceNr, out DataTable dtErrorConf)
        //  {
        //  dtErrorConf = null;
        //  try {
        //    dtErrorConf = FillDataTableBySQL("EXECUTE DeviceErrorGetErrorConfigurationByPart " + myID_SubWS + ", " + TraceNr);
        //    return (dtErrorConf != null && dtErrorConf.Rows.Count == 1);
        //    }
        //  catch (Exception e) {
        //    LastErrorDescription = "DeviceErrorGetErrorConfigurationByPart: " + e.ToString();
        //    return false;
        //    }
        //  }
        #endregion

        #region Station Methods - Samples
        public bool Samples_CheckSample(long TraceNr)
        {
            return Samples_CheckSample(TraceNr, myIDWS, mySubWS);
        }
        public bool Samples_CheckSample(long TraceNr, out Sample sample /*bool IsSample, out bool IsSampleForThisWS, out string ErrorCode, out string SampleDetails, out bool SampleOk*/)
        {/*
      IsSample = false;
      IsSampleForThisWS = false;
      ErrorCode = "";
      SampleDetails = "";
      SampleOk = false;*/
            sample = new Sample();
            return Samples_CheckSample(TraceNr, myIDWS, mySubWS, out sample);
        }
        public bool Samples_ValidateSampleTesting(long TraceNr)
        {
            return Samples_ValidateSampleTesting(TraceNr, myIDSubWS);
        }
        #endregion

        #region Station Methods - Calibration
        public bool Calibration_CheckCaliber(long TraceNr)
        {
            return Calibration_CheckCaliber(TraceNr, myIDWS, mySubWS);
        }
        public bool Calibration_CheckCaliber(long TraceNr, out bool IsCaliber, out bool IsCaliberForThisWS)
        {
            IsCaliber = false;
            IsCaliberForThisWS = false;
            return Calibration_CheckCaliber(TraceNr, myIDWS, mySubWS, out IsCaliber, out IsCaliberForThisWS);
        }
        public List<CalibrationMeasurement> Calibration_GetMeasurementList(long TraceNr)
        {
            return Calibration_GetMeasurementList(TraceNr, myIDWS, mySubWS);
        }
        public List<CalibrationAdjustment> Calibration_GetLastAdjustments()
        {
            return Calibration_GetLastAdjustments(myIDWS, mySubWS);
        }
        #endregion

        #region Station Methods - MeasureLimits
        public bool Measures_LoadMeasureLimits()
        {
            if (myPrehReference == null) return false;
            return Measures_LoadMeasureLimits(myPrehReference.IDRef);
        }
        public bool Measures_LoadMeasureLimits(int IDRef)
        {
            if (!SetReference(IDRef)) return false;
            return Measures_LoadMeasureLimits(IDRef, myIDSubWS);
        }
        public bool Measures_LoadGenericMeasureLimits()
        {
            return Measures_LoadMeasureLimits(0, myIDSubWS);
        }
        /// <summary>
        /// Updates a MeasureLimit without saving changes in the Log.
        /// </summary>
        public bool Measures_UpdateMeasureLimit(int ID_MeasureLimit, double MinValue, double MaxValue, string Unit, string Variant, bool Active, string Description)
        {
            //if (myIDRef <= 0) return false;
            if (myPrehReference.IDRef <= 0) return false;
            return Measures_UpdateMeasureLimit(-1, -1, myIDSubWS, ID_MeasureLimit, MinValue, MaxValue, Unit, Variant, Active, Description);
        }
        /// <summary>
        /// Updates a MeasureLimit and saves the change to a change log.
        /// </summary>
        public bool Measures_UpdateMeasureLimit(int ID_User, int ID_Ref, int ID_MeasureLimit, double MinValue, double MaxValue, string Unit, string Variant, bool Active, string Description)
        {
            //if (myIDRef <= 0) return false;
            if (myPrehReference.IDRef <= 0) return false;
            return Measures_UpdateMeasureLimit(-1, -1, myIDSubWS, ID_MeasureLimit, MinValue, MaxValue, Unit, Variant, Active, Description);
        }
        #endregion

        #region Station Methods - System Configurations
        public bool SysConfig_LoadSystemConfigurations()
        {
            if (myPrehReference == null) return false;
            return SysConfig_LoadSystemConfigurations(myPrehReference.IDRef, myIDSubWS);
        }
        public bool SysConfig_LoadSystemConfigurations(int IDRef)
        {
            if (!SetReference(IDRef)) return false;
            return SysConfig_LoadSystemConfigurations(IDRef, myIDSubWS);
        }
        public bool SysConfig_LoadGenericSystemConfigurations()
        {
            return SysConfig_LoadSystemConfigurations(myIDSubWS, 0);
        }
        #endregion


        //Para utilizar na maquina que monta a o LCD no PCB (OP05)
        public bool GetLCDInspectionData(int ID_FinalRef, int ID_SubWS, out Reference RefLCD, out int ProgIFM1, out int ProgIFM2)
        {
            ProgIFM1 = -1;
            ProgIFM2 = -1;
            RefLCD = new Reference();

            try
            {
                PPTraceDB myDB = new PPTraceDB(sb_DefaultDBConnectionString);

                IEnumerable<IDataRecord> readerlist = myDB.FillDataReaderBySQL(new SqlCommand("EXECUTE [GetLCDRef] " + ID_FinalRef));
                foreach (var reader in readerlist)
                {
                    RefLCD = new Reference
                    {
                        IDRef = (int)reader["ID_Ref"],
                        IDRefProfile = reader["ID_RefProfile"] == DBNull.Value ? -1 : (int)reader["ID_RefProfile"],
                        IDRefSeqGroup = reader["ID_RefSeqGroup"] == DBNull.Value ? -1 : (int)reader["ID_RefSeqGroup"],
                        ExtraCode = reader["ExtraCode"] == DBNull.Value ? "" : (string)reader["ExtraCode"],
                        RefPreh = (string)reader["RefPreh"],
                        RefDescription = reader["RefDescription"] == DBNull.Value ? "" : (string)reader["RefDescription"],
                        RefShortType = reader["RefShortType"] == DBNull.Value ? "" : (string)reader["RefShortType"],
                        Active = (bool)reader["Active"],
                    };
                }

                myDB.Measures_LoadMeasureLimits(RefLCD.IDRef, ID_SubWS);
                MeasureLimit IFM1 = myDB.Measures_GetLimits("ProgIFM1");
                MeasureLimit IFM2 = myDB.Measures_GetLimits("ProgIFM2");

                if (IFM1 == null || IFM2 == null) return false;

                ProgIFM1 = (int)IFM1.MinValue;
                ProgIFM2 = (int)IFM2.MinValue;
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }




    }
}

