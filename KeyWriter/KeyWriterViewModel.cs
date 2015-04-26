using System;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace KeyWriter
{
    public class KeyWriterViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private ObservableCollection<DogTypeViewModel> _dogTypes;
        private string _superDogGuid;
        private string _machineCode;
        private string _createDate;
        private string _updateDate;
        private string _expireDate;
        private int _manufacturerId;
        private string _remark;

        public ObservableCollection<DogTypeViewModel> DogTypes
        {
            get
            {
                return _dogTypes;
            }
            set
            {
                _dogTypes = value;
                OnPropertyChanged("SuperDogTypes");
            }
        }

        public string SuperDogGuid
        {
            get
            {
                return _superDogGuid;
            }
            set
            {
                _superDogGuid = value;
                OnPropertyChanged("SuperDogGuid");
            }
        }

        public string MachineCode
        {
            get
            {
                return _machineCode;
            }
            set
            {
                _machineCode = value;
                OnPropertyChanged("MachineCode");
            }
        }

        public string CreateDate
        {
            get
            {
                return _createDate;
            }
            set
            {
                _createDate = value;
                OnPropertyChanged("CreateDate");
            }
        }

        public string UpdateDate
        {
            get
            {
                return _updateDate;
            }
            set
            {
                _updateDate = value;
                OnPropertyChanged("UpdateDate");
            }
        }

        public string ExpireDate
        {
            get
            {
                return _expireDate;
            }
            set
            {
                _expireDate = value;
                OnPropertyChanged("ExpireDate");
            }
        }

        public int ManufacturerId
        {
            get
            {
                return _manufacturerId;
            }
            set
            {
                _manufacturerId = value;
                OnPropertyChanged("ManufacturerId");
            }
        }

        public string Remark
        {
            get
            {
                return _remark;
            }
            set
            {
                _remark = value;
                OnPropertyChanged("Remark");
            }
        }

        public KeyWriterViewModel()
        {
            _dogTypes = new ObservableCollection<DogTypeViewModel>();
        }
    }

    public class DogTypeViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private bool _isIncluded;
        private int _typeValue;
        private string _typeName;

        public bool IsIncluded
        {
            get
            {
                return _isIncluded;
            }
            set
            {
                _isIncluded = value;
                OnPropertyChanged("IsIncluded");
            }
        }

        public int TypeValue
        {
            get
            {
                return _typeValue;
            }
            set
            {
                _typeValue = value;
                OnPropertyChanged("TypeValue");
            }
        }

        public string TypeName
        {
            get
            {
                return _typeName;
            }
            set
            {
                _typeName = value;
                OnPropertyChanged("TypeName");
            }
        }

        public DogTypeViewModel(bool isIncluded, int typeValue, string typeName)
        {
            _isIncluded = isIncluded;
            _typeValue = typeValue;
            _typeName = typeName;
        }
    }
}
