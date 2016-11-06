using System;
using System.Collections.Generic;
using System.ComponentModel;
using LogControl9K.Annotations;

namespace LogControl9K.Windows.Util {
    /// <summary>
    /// <para>Базовый класс для ViewModel'ей,</para>
    /// <para>реализует INotifyPropertyChanged и</para>
    /// <para>имеет метод для передачи параметра для View Model от View (AddParameter)</para>
    /// </summary>
    class ViewModelBase : INotifyPropertyChanged {


        #region Поля

        /// <summary>
        /// Словарь параметров
        /// </summary>
        private Dictionary<string, object> _parametersDictionary = new Dictionary<string, object>();

        #endregion


        #region Публичные методы

        /// <summary>
        /// Передать параметр во ViewModel из View
        /// </summary>
        /// <param name="parameterName">Имя параметра</param>
        /// <param name="parameterValue">Значение параметра</param>
        public void AddParameter(string parameterName, object parameterValue) {
            if (!_parametersDictionary.ContainsKey(parameterName)) {
                _parametersDictionary.Add(parameterName, parameterValue);
                OnParameterAdded();
            } 
        }

        #endregion

        
        #region Защищённые методы


        /// <summary>
        /// Получить параметр, метод вызывается во ViewModel
        /// </summary>
        /// <param name="parameterName"></param>
        /// <returns></returns>
        protected object GetParameter(string parameterName) {
            if (_parametersDictionary.ContainsKey(parameterName)) {
                return _parametersDictionary[parameterName];
            }
            return null;
        }
        
        #endregion
        

        #region События


        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName) {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion
        

        #region ParameterAdded

        /// <summary>
        /// Событие передачи параметра от View к ViewModel
        /// </summary>
        public event EventHandler ParameterAdded;

        protected virtual void OnParameterAdded() {
            EventHandler handler = ParameterAdded;
            if (handler != null) handler(this, EventArgs.Empty);
        }
        
        #endregion


        #endregion


    }
}
