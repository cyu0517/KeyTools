using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using IT.License;

namespace KeyWriter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class KeyWriterWindow : Window
    {
        private readonly KeyWriterViewModel _keyWriterViewModel;
        private LicenseHandler _licenseHandler;
        private SuperDogDb _superDogDb;

        public KeyWriterWindow()
        {
            InitializeComponent();

            _keyWriterViewModel = new KeyWriterViewModel();
            DataContext = _keyWriterViewModel;
        }

        private void ClearViewModel()
        {
            foreach (var each in _keyWriterViewModel.DogTypes)
            {
                each.IsIncluded = false;
            }

            _keyWriterViewModel.SuperDogGuid = "";
            _keyWriterViewModel.MachineCode = "";
            _keyWriterViewModel.CreateDate = "";
            _keyWriterViewModel.UpdateDate = "";
            _keyWriterViewModel.ExpireDate = "";
            _keyWriterViewModel.ManufacturerId = 0;
            _keyWriterViewModel.User = "";
            _keyWriterViewModel.Remark = "";
        }

        private bool SuperDogIsInitialized()
        {
            if (!_licenseHandler.IsInitialized())
            {
                MessageBox.Show("超级狗尚未导入初始许可");
                return false;
            }

            return true;
        }

        private void GetMachineCode()
        {
            var machineCode = _licenseHandler.GetMachineCode();
            if (!string.IsNullOrEmpty(machineCode))
            {
                _keyWriterViewModel.MachineCode = machineCode;
            }
            else
            {
                MessageBox.Show("超级狗中尚未存储机器特征码");
            }
        }

        private void SuperDogFormat()
        {
            var message = _licenseHandler.CheckMaster();
            if (!string.IsNullOrEmpty(message))
            {
                MessageBox.Show(message);
                return;
            }

            message = _licenseHandler.Check();
            if (!string.IsNullOrEmpty(message))
            {
                MessageBox.Show(message);
                return;
            }

            if (MessageBox.Show("确定将超级狗恢复出厂状态导入初始许可？", "", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                ClearViewModel();

                var oldSuperDogGuid = _licenseHandler.GetSuperDogGuid();
                if (!string.IsNullOrEmpty(oldSuperDogGuid))
                {
                    _superDogDb.RemoveSuperDog(oldSuperDogGuid);
                }

                if (_licenseHandler.Format(out message))
                {
                    string newSuperDogGuid;

                    do
                    {
                        newSuperDogGuid = Guid.NewGuid().ToString().Replace("-", "");
                        Thread.Sleep(200);
                    } while (_superDogDb.GetSuperDog(newSuperDogGuid) != null);

                    if (_licenseHandler.SetSuperDogGuid(newSuperDogGuid))
                    {
                        _keyWriterViewModel.SuperDogGuid = newSuperDogGuid;

                        var superDogDate = _licenseHandler.GetSuperDogDate();

                        var superDog = new SuperDog
                            {
                                SuperDogGuid = newSuperDogGuid,
                                CreateDate = superDogDate.ToString("yyyy-MM-dd"),
                                UpdateDate = superDogDate.ToString("yyyy-MM-dd")
                            };

                        superDog = _superDogDb.AddSuperDog(superDog);

                        _keyWriterViewModel.CreateDate = superDog.CreateDate;
                        _keyWriterViewModel.UpdateDate = superDog.UpdateDate;
                    }
                    else
                    {
                        message = "超级狗唯一标识写入失败，格式化未完成";
                    }
                }

                MessageBox.Show(message);
            }
        }

        private void SuperDogRead()
        {
            try
            {
                foreach (var each in _keyWriterViewModel.DogTypes)
                {
                    if (_licenseHandler.IsIncludedDogType((DogType) each.TypeValue))
                    {
                        each.IsIncluded = true;
                    }
                    else
                    {
                        each.IsIncluded = false;
                    }
                }

                _keyWriterViewModel.SuperDogGuid = _licenseHandler.GetSuperDogGuid();
                _keyWriterViewModel.MachineCode = _licenseHandler.GetMachineCode();

                var expireDate = _licenseHandler.GetExpireDate();
                if (expireDate != new DateTime(1970, 1, 1))
                {
                    _keyWriterViewModel.ExpireDate = expireDate.ToString("yyyy-MM-dd");
                }

                var superDog = _superDogDb.GetSuperDog(_keyWriterViewModel.SuperDogGuid);
                if (superDog != null)
                {
                    _keyWriterViewModel.CreateDate = superDog.CreateDate;
                    _keyWriterViewModel.UpdateDate = superDog.UpdateDate;
                    _keyWriterViewModel.ManufacturerId = superDog.ManufacturerId;
                    _keyWriterViewModel.User = superDog.User;
                    _keyWriterViewModel.Remark = superDog.Remark;
                }

                MessageBox.Show("读取完成");
            }
            catch
            {
                MessageBox.Show("读取超级狗信息发生错误");
            }
        }

        private void SuperDogWrite()
        {
            if (string.IsNullOrEmpty(_keyWriterViewModel.SuperDogGuid))
            {
                MessageBox.Show("尚未读取超级狗信息");
                return;
            }

            var superDogType = 0;

            foreach (var each in _keyWriterViewModel.DogTypes)
            {
                if (each.IsIncluded)
                {
                    superDogType += each.TypeValue;
                }
            }

            if (superDogType == 0)
            {
                MessageBox.Show("尚未设置超级狗类型");
                return;
            }

            if (string.IsNullOrEmpty(_keyWriterViewModel.MachineCode))
            {
                MessageBox.Show("尚未设置机器特征码");
                return;
            }

            if (string.IsNullOrEmpty(_keyWriterViewModel.ExpireDate))
            {
                MessageBox.Show("尚未设置过期日期");
                return;
            }

            if (MessageBox.Show("确定将信息写入超级狗？", "", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                Manufacturer manufacturer = null;
                if (CbxManufacturer.SelectedIndex == -1 && !string.IsNullOrEmpty(CbxManufacturer.Text))
                {
                    manufacturer = _superDogDb.AddManufacturer(new Manufacturer {Name = CbxManufacturer.Text});
                    if (manufacturer != null)
                    {
                        _keyWriterViewModel.Manufacturers.Add(new ManufacturerViewModel(manufacturer.Id, manufacturer.Name));
                        _keyWriterViewModel.ManufacturerId = manufacturer.Id;
                    }
                }

                var flagSetSuperDogType = _licenseHandler.SetSuperDogType(superDogType);
                var flagSetMachineCode = _licenseHandler.SetMachineCode(_keyWriterViewModel.MachineCode);
                var flagSetExpireDate = _licenseHandler.SetExpireDate(Convert.ToDateTime(_keyWriterViewModel.ExpireDate));
                var flagSetManufacturerId = _licenseHandler.SetManufacturerId(manufacturer != null ? manufacturer.Id : 0);

                if (!flagSetSuperDogType)
                {
                    MessageBox.Show("写入超级狗类型发生错误");
                }

                if (!flagSetMachineCode)
                {
                    MessageBox.Show("写入机器特征码发生错误");
                }

                if (!flagSetExpireDate)
                {
                    MessageBox.Show("写入过期日期发生错误");
                }

                if (!flagSetManufacturerId)
                {
                    MessageBox.Show("写入厂商发生错误");
                }

                if (flagSetSuperDogType && flagSetMachineCode && flagSetExpireDate && flagSetManufacturerId)
                {
                    var superDog = new SuperDog
                    {
                        SuperDogGuid = _keyWriterViewModel.SuperDogGuid,
                        SuperDogType = superDogType,
                        MachineCode = _keyWriterViewModel.MachineCode,
                        UpdateDate = _licenseHandler.GetSuperDogDate().ToString("yyyy-MM-dd"),
                        ExpireDate = _keyWriterViewModel.ExpireDate,
                        ManufacturerId = _keyWriterViewModel.ManufacturerId,
                        User = _keyWriterViewModel.User,
                        Remark = _keyWriterViewModel.Remark
                    };

                    _superDogDb.ModifySuperDog(superDog);
                }

                MessageBox.Show("写入完成");
            }
            catch
            {
                MessageBox.Show("写入超级狗信息发生错误");
            }
        }

        private void KeyWriterWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (DogType type in Enum.GetValues(typeof(DogType)))
                {
                    _keyWriterViewModel.DogTypes.Add(new DogTypeViewModel(false, (int)type, type.ToString()));
                }

                _licenseHandler = LicenseHandler.Instance;

                var message = _licenseHandler.CheckMaster();
                if (!string.IsNullOrEmpty(message))
                {
                    MessageBox.Show(message);
                    Close();
                    return;
                }

                message = _licenseHandler.Check();
                if (!string.IsNullOrEmpty(message))
                {
                    MessageBox.Show(message);
                    Close();
                }

                _superDogDb = new SuperDogDb("SuperDogDb.sqlite");

                var manufacturers = _superDogDb.GetManufacturers();
                if (manufacturers != null)
                {
                    foreach (var manufacturer in manufacturers)
                    {
                        _keyWriterViewModel.Manufacturers.Add(new ManufacturerViewModel(manufacturer.Id, manufacturer.Name));
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void CbDogTypes_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (var dogType in _keyWriterViewModel.DogTypes)
                {
                    if (CbDogTypes.IsChecked != null)
                    {
                        dogType.IsIncluded = (bool) CbDogTypes.IsChecked;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var message = _licenseHandler.CheckMaster();
                if (!string.IsNullOrEmpty(message))
                {
                    MessageBox.Show(message);
                    ClearViewModel();
                    return;
                }

                message = _licenseHandler.Check();
                if (!string.IsNullOrEmpty(message))
                {
                    MessageBox.Show(message);
                    ClearViewModel();
                    return;
                }

                if (!SuperDogIsInitialized())
                {
                    ClearViewModel();
                    return;
                }

                var button = (Button) sender;

                switch (button.Name)
                {
                    case "BtnGetMachineCode":
                        GetMachineCode();
                        break;
                    case "BtnRead":
                        SuperDogRead();
                        break;
                    case "BtnWrite":
                        SuperDogWrite();
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void BtnFormat_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                SuperDogFormat();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
