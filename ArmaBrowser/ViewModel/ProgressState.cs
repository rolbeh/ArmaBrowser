using System;
using ArmaBrowser.Logic;

namespace ArmaBrowser.ViewModel
{
    // ReSharper disable once UnusedMember.Global
    internal class ProgressState : ObjectNotify, IProgressState
    {
        private int _maximum;
        private int _minimum;
        private int _value;

        public int Maximum
        {
            get => _maximum;
            set
            {
                if (value == _maximum) return;
                _maximum = value;
                OnPropertyChanged();
            }
        }

        public int Minimum
        {
            get => _minimum;
            set
            {
                if (value == _minimum) return;
                _minimum = value;
                OnPropertyChanged();
            }
        }

        public int Value
        {
            get => _value;
            set
            {
                if (value == _value) return;
                _value = value;
                OnPropertyChanged();
            }
        }
    }
}