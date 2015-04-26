using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Text;
using SuperDog;
using Sentinel.Ldk.LicGen;

namespace IT.License
{
    public class LicenseHandler : IDisposable
    {
        private static LicenseHandler _instance;
        private static readonly object InstanceLocker = new object();

        private Dog _dog;
        private LicGenAPIHelper _licGenApiHelper;

        public static LicenseHandler Instance
        {
            get
            {
                lock (InstanceLocker)
                {
                    return _instance ?? (_instance = new LicenseHandler());
                }
            }
        }

        private bool CheckDogStatus()
        {
            if (_dog == null || !_dog.IsLoggedIn() || !_dog.IsValid())
            {
                if (!Login())
                {
                    return false;
                }
            }

            return true;
        }

        private string ReadFile(int dogFileId)
        {
            if (!CheckDogStatus())
            {
                return null;
            }

            var dogFile = _dog.GetFile(dogFileId);

            if (dogFile == null || !dogFile.IsValid())
            {
                return null;
            }

            var size = 0;
            var dogStatus = dogFile.FileSize(ref size);
            if (dogStatus != DogStatus.StatusOk)
            {
                return null;
            }

            var buffer = new byte[size];
            dogStatus = dogFile.Read(buffer, 0, buffer.Length);
            if (dogStatus != DogStatus.StatusOk)
            {
                return null;
            }

            return Encoding.UTF8.GetString(buffer).Replace("\0", "");
        }

        private bool WriteFile(int dogFileId, string content)
        {
            if (!CheckDogStatus())
            {
                return false;
            }

            var dogFile = _dog.GetFile(dogFileId);

            if (dogFile == null || !dogFile.IsValid())
            {
                return false;
            }

            var buffer = Encoding.UTF8.GetBytes(content);
            var dogStatus = dogFile.Write(buffer, 0, buffer.Count());

            return dogStatus == DogStatus.StatusOk;
        }

        private bool Login()
        {
            _dog = new Dog(DogFeature.Default);

            if (_dog.Login(SuperDogVendorCode.VendorCode) == DogStatus.StatusOk && _dog.IsValid())
            {
                string info = null;
                if (_dog.GetSessionInfo(Dog.KeyInfo, ref info) == DogStatus.StatusOk)
                {
                    return true;
                }
            }

            return false;
        }

        private string LoadFile(string filename)
        {
            var file = new FileInfo(filename);

            if (!file.Exists)
            {
                return null;
            }

            var fs = file.OpenText();
            var length = (int) file.Length;
            var temp = new char[file.Length];

            fs.Read(temp, 0, length);
            fs.Close();

            return new string(temp);
        }

        public string CheckMaster(string license = null)
        {
            if (!CheckDogStatus())
            {
                return "未检测到超级狗";
            }

            string info = null;
            var dogStatus = _dog.GetSessionInfo(Dog.UpdateInfo, ref info);
            if (dogStatus != DogStatus.StatusOk || string.IsNullOrEmpty(info))
            {
                return "超级狗信息读取失败";
            }

            _licGenApiHelper = new LicGenAPIHelper();

            var licGenStatus = _licGenApiHelper.sntl_lg_initialize(null);
            if (licGenStatus != sntl_lg_status_t.SNTL_LG_STATUS_OK)
            {
                _licGenApiHelper.sntl_lg_cleanup();
                return "初始化许可失败";
            }

            licGenStatus = _licGenApiHelper.sntl_lg_start(null, SuperDogVendorCode.VendorCode,
                sntl_lg_license_type_t.SNTL_LG_LICENSE_TYPE_FORMAT_AND_UPDATE, license, info);
            if (licGenStatus != sntl_lg_status_t.SNTL_LG_STATUS_OK)
            {
                _licGenApiHelper.sntl_lg_cleanup();

                switch (licGenStatus)
                {
                    case sntl_lg_status_t.SNTL_LG_INVALID_VENDOR_CODE:
                        return "无效的开发商代码";
                    case sntl_lg_status_t.SNTL_LG_MASTER_KEY_IO_ERROR:
                        return "未检测到开发狗";
                    case sntl_lg_status_t.SNTL_LG_MASTER_KEY_CONNECT_ERROR:
                    case sntl_lg_status_t.SNTL_LG_MASTER_KEY_ACCESS_ERROR:
                        return "连接开发狗发生错误";
                    default:
                        return "加载许可失败";
                }
            }

            if (string.IsNullOrEmpty(license))
            {
                _licGenApiHelper.sntl_lg_cleanup();
            }

            return null;
        }

        public string Check()
        {
            _dog = new Dog(DogFeature.Default);

            var dogStatus = _dog.Login(SuperDogVendorCode.VendorCode);
            if (dogStatus == DogStatus.StatusOk)
            {
                return null;
            }

            switch (dogStatus)
            {
                case DogStatus.DogNotFound:
                    return "未检测到超级狗";
                case DogStatus.DeviceError:
                    return "超级狗USB通信错误";
                case DogStatus.LocalCommErr:
                    return "超级狗本地环境通讯错误";
                case DogStatus.TooOldLM:
                    return "超级狗运行环境版本过低";
                case DogStatus.InvalidVendorCode:
                    return "无效的开发商代码";
                case DogStatus.UnknownVcode:
                    return "未知的开发商代码";
                default:
                    return "检测超级狗失败";
            }
        }

        public bool Format(out string message)
        {
            var license = LoadFile("License.xml");

            message = CheckMaster(license);
            if (!string.IsNullOrEmpty(message))
            {
                return false;
            }

            message = Check();
            if (!string.IsNullOrEmpty(message))
            {
                return false;
            }

            if (!CheckDogStatus())
            {
                message = "超级狗状态不正确";
                return false;
            }

            string lgLicense = null;
            string lgStatus = null;
            var status = _licGenApiHelper.sntl_lg_generate_license(null, ref lgLicense, ref lgStatus);
            _licGenApiHelper.sntl_lg_cleanup();
            if (status != sntl_lg_status_t.SNTL_LG_STATUS_OK)
            {
                message = "生成许可失败";
                return false;
            }

            string acknowledgeXml = null;
            var dogStatus = Dog.Update(lgLicense, ref acknowledgeXml);

            _dog.Logout();
            _dog.Dispose();
            _dog = null;

            if (dogStatus != DogStatus.StatusOk)
            {
                message = "格式化超级狗失败";
                return false;
            }

            message = "格式化成功";
            return true;
        }

        public bool IsInitialized()
        {
            if (!CheckDogStatus())
            {
                return false;
            }

            var superDogGuid = GetSuperDogGuid();
            if (string.IsNullOrEmpty(superDogGuid))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 获取压缩码
        /// </summary>
        /// <returns>压缩码</returns>
        public string GetCompressiCode()
        {
            return ReadFile(DogFileId.CompressiCode);
        }

        /// <summary>
        /// 获取数据库用户名
        /// </summary>
        /// <returns>数据库用户名</returns>
        public string GetDatabaseUserName()
        {
            return ReadFile(DogFileId.DatabaseUserName);
        }

        /// <summary>
        /// 获取数据库密码
        /// </summary>
        /// <returns>数据库密码</returns>
        public string GetDatabasePassword()
        {
            return ReadFile(DogFileId.DatabasePassword);
        }

        /// <summary>
        /// 获取AES秘钥
        /// </summary>
        /// <returns>AES秘钥</returns>
        public string GetAes()
        {
            return ReadFile(DogFileId.Aes);
        }

        /// <summary>
        /// 获取超级狗唯一标识
        /// </summary>
        /// <returns>超级狗唯一标识</returns>
        public string GetSuperDogGuid()
        {
            return ReadFile(DogFileId.SuperDogGuid);
        }

        /// <summary>
        /// 获取超级狗类型
        /// </summary>
        /// <returns>超级狗类型</returns>
        public uint GetSuperDogType()
        {
            var dogTypeString = ReadFile(DogFileId.SuperDogType);
            if (!string.IsNullOrEmpty(dogTypeString))
            {
                uint dogType;
                if (uint.TryParse(dogTypeString, out dogType))
                {
                    return dogType;
                }
            }

            return 0;
        }

        /// <summary>
        /// 获取厂商标识
        /// </summary>
        /// <returns>厂商标识</returns>
        public int GetManufacturerId()
        {
            var manufacturerIdString = ReadFile(DogFileId.ManufacturerId);
            if (!string.IsNullOrEmpty(manufacturerIdString))
            {
                try
                {
                    return Convert.ToInt32(manufacturerIdString, 16);
                }
                catch
                {
                    return 0;
                }
            }

            return 0;
        }

        /// <summary>
        /// 获取超级狗过期日期时间
        /// </summary>
        /// <returns>超级狗过期日期时间</returns>
        public DateTime GetExpireDate()
        {
            var content = ReadFile(DogFileId.ExpireDate);

            if (!string.IsNullOrEmpty(content) && content.Length == 8)
            {
                var dateString = string.Format("{0}-{1}-{2}", content.Substring(0, 4), content.Substring(4, 2), content.Substring(6, 2));
                DateTime expireDate;
                if (DateTime.TryParse(dateString, out expireDate))
                {
                    return expireDate;
                }
            }

            return new DateTime(1970, 1, 1);
        }

        /// <summary>
        /// 获取机器特征码
        /// </summary>
        /// <returns>机器特征码</returns>
        public string GetMachineCode()
        {
            return ReadFile(DogFileId.MachineCode);
        }

        /// <summary>
        /// 获取超级狗虚拟日期时间
        /// </summary>
        /// <returns>超级狗虚拟日期时间</returns>
        public DateTime GetSuperDogDate()
        {
            if (!CheckDogStatus())
            {
                return new DateTime(1970, 1, 1);
            }

            var superDogDate = new DateTime(1970, 1, 1);

            if (_dog.GetTime(ref superDogDate) != DogStatus.StatusOk)
            {
                return new DateTime(1970, 1, 1);
            }

            return superDogDate.AddHours(8);
        }

        /// <summary>
        /// 设置压缩码
        /// </summary>
        /// <returns>是否成功</returns>
        public bool SetCompressiCode(string compressiCode)
        {
            return WriteFile(DogFileId.CompressiCode, compressiCode);
        }

        /// <summary>
        /// 设置数据库用户名
        /// </summary>
        /// <returns>是否成功</returns>
        public bool SetDatabaseUserName(string databaseUserName)
        {
            return WriteFile(DogFileId.DatabaseUserName, databaseUserName);
        }

        /// <summary>
        /// 设置数据库密码
        /// </summary>
        /// <returns>是否成功</returns>
        public bool SetDatabasePassword(string databasePassword)
        {
            return WriteFile(DogFileId.DatabasePassword, databasePassword);
        }

        /// <summary>
        /// 设置AES秘钥
        /// </summary>
        /// <returns>是否成功</returns>
        public bool SetAes(string aes)
        {
            return WriteFile(DogFileId.Aes, aes);
        }

        /// <summary>
        /// 设置超级狗唯一标识
        /// </summary>
        /// <returns>是否成功</returns>
        public bool SetSuperDogGuid(string superDogGuid)
        {
            return WriteFile(DogFileId.SuperDogGuid, superDogGuid);
        }

        /// <summary>
        /// 设置超级狗类型
        /// </summary>
        /// <returns>是否成功</returns>
        public bool SetSuperDogType(int superDogType)
        {
            return WriteFile(DogFileId.SuperDogType, superDogType.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// 设置厂商
        /// </summary>
        /// <returns>是否成功</returns>
        public bool SetManufacturerId(int manufacturerId)
        {
            return WriteFile(DogFileId.ManufacturerId, Convert.ToString(manufacturerId, 16));
        }

        /// <summary>
        /// 设置超级狗过期日期时间
        /// </summary>
        /// <returns>是否成功</returns>
        public bool SetExpireDate(DateTime expireDate)
        {
            return WriteFile(DogFileId.ExpireDate, expireDate.ToString("yyyyMMdd"));
        }

        /// <summary>
        /// 设置机器特征码
        /// </summary>
        /// <returns>是否成功</returns>
        public bool SetMachineCode(string machineCode)
        {
            return WriteFile(DogFileId.MachineCode, machineCode);
        }

        /// <summary>
        /// 判断超级狗是否具有FS类型
        /// </summary>
        /// <returns></returns>
        public bool IsFieldScripterSuperDog()
        {
            return (GetSuperDogType() & (uint) DogType.FieldScripter) > 0;
        }

        /// <summary>
        /// 判断超级狗是否具有FT类型
        /// </summary>
        /// <returns></returns>
        public bool IsFieldTesterSuperDog()
        {
            return (GetSuperDogType() & (uint) DogType.FieldTester) > 0;
        }

        /// <summary>
        /// 判断超级狗是否具有CP类型
        /// </summary>
        /// <returns></returns>
        public bool IsFieldPackagerSuperDog()
        {
            return (GetSuperDogType() & (uint) DogType.FieldPackager) > 0;
        }

        /// <summary>
        /// 判断超级狗是否包含指定类型
        /// </summary>
        /// <param name="dogType">超级狗类型</param>
        /// <returns></returns>
        public bool IsIncludedDogType(DogType dogType)
        {
            return (GetSuperDogType() & (uint) dogType) > 0;
        }

        /// <summary>
        /// 判断超级狗是否具已过期
        /// </summary>
        /// <returns></returns>
        public bool IsExpired()
        {
            var superDogDate = GetSuperDogDate();
            var expireDate = GetExpireDate();

            return (expireDate - superDogDate).TotalDays < 0;
        }

        /// <summary>
        /// 获取当前PC机器特征码
        /// </summary>
        /// <returns>当前PC机器特征码</returns>
        public string GetCurrentMachineCode()
        {
            var codeString = string.Empty;

            try
            {
                var mc = new ManagementClass("Win32_BIOS");
                var moc = mc.GetInstances();

                foreach (ManagementObject mo in moc)
                {
                    codeString = mo.Properties["SerialNumber"].Value.ToString();
                    break;
                }

                moc.Dispose();

                var adapters = NetworkInterface.GetAllNetworkInterfaces();
                foreach (var adapter in adapters)
                {
                    if (adapter.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                    {
                        codeString += adapter.GetPhysicalAddress();
                    }
                }

                mc = new ManagementClass("Win32_Processor");
                moc = mc.GetInstances();
                foreach (ManagementObject mo in moc)
                {
                    codeString += mo.Properties["ProcessorId"].Value.ToString();
                    break;
                }

                moc.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return Md5.GetStringMd5(codeString);
        }

        /// <summary>
        /// 判断当前PC机器特征码与超级狗中存储的机器特征码是否一致
        /// </summary>
        /// <returns></returns>
        public bool IsBindingMachine()
        {
            var machineCode = GetMachineCode();
            var currentMachineCode = GetCurrentMachineCode();

            return !string.IsNullOrEmpty(machineCode)
                && !string.IsNullOrEmpty(currentMachineCode)
                && machineCode.Equals(currentMachineCode);
        }

        public void Dispose()
        {
            if (_dog != null)
            {
                _dog.Logout();
            }
        }
    }
}
