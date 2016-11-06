using System;
using System.ComponentModel;
using System.Threading;
using System.Windows.Input;
using System.Windows.Threading;

// ReSharper disable InconsistentNaming

//using LogicManageSpace;

namespace LogControl9K.Windows.Util
{

    //-------------------------------------------------------------------------------------------
    // классы типов команд


    /// <summary>
    /// The ViewModelCommand class - an ICommand that can fire a function.
    /// </summary>
    public class sCommand : ICommand
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="sCommand"/> class.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="canExecute">if set to <c>true</c> [can execute].</param>
        public sCommand(Action action, bool canExecute = true)
        {
            //  Set the action.
            this.action = action;
            this.canExecute = canExecute;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="sCommand"/> class.
        /// </summary>
        /// <param name="parameterizedAction">The parameterized action.</param>
        /// <param name="canExecute">if set to <c>true</c> [can execute].</param>
        public sCommand(Action<object> parameterizedAction, bool canExecute = true)
        {
            //  Set the action.
            this.parameterizedAction = parameterizedAction;
            this.canExecute = canExecute;
        }

        /// <summary>
        /// Executes the command.
        /// </summary>
        /// <param name="param">The param.</param>
        public virtual void DoExecute(object param)
        {
            //  Invoke the executing command, allowing the command to be cancelled.
            CancelCommandEventArgs args = new CancelCommandEventArgs() { Parameter = param, Cancel = false };
            InvokeExecuting(args);

            //  If the event has been cancelled, bail now.
            if (args.Cancel)
                return;

            //  Call the action or the parameterized action, whichever has been set.
            InvokeAction(param);

            //  Call the executed function.
            InvokeExecuted(new CommandEventArgs() { Parameter = param });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="param"></param>
        protected void InvokeAction(object param)
        {
            Action theAction = action;
            Action<object> theParameterizedAction = parameterizedAction;
            if (theAction != null)
                theAction();
            else if (theParameterizedAction != null)
                theParameterizedAction(param);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        protected void InvokeExecuted(CommandEventArgs args)
        {
            CommandEventHandler executed = Executed;

            //  Call the executed event.
            if (executed != null)
                executed(this, args);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        protected void InvokeExecuting(CancelCommandEventArgs args)
        {
            CancelCommandEventHandler executing = Executing;

            //  Call the executed event.
            if (executing != null)
                executing(this, args);
        }


        /// <summary>
        /// The action (or parameterized action) that will be called when the command is invoked.
        /// </summary>
        protected Action action = null;

        /// <summary>
        /// 
        /// </summary>
        protected Action<object> parameterizedAction = null;

        /// <summary>
        /// Bool indicating whether the command can execute.
        /// </summary>
        private bool canExecute = false;

        /// <summary>
        /// Gets or sets a value indicating whether this instance can execute.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance can execute; otherwise, <c>false</c>.
        /// </value>
        public bool CanExecute
        {
            get { return canExecute; }
            set
            {
                if (canExecute != value)
                {
                    canExecute = value;
                    EventHandler canExecuteChanged = CanExecuteChanged;
                    if (canExecuteChanged != null)
                        canExecuteChanged(this, EventArgs.Empty);
                }
            }
        }

        #region ICommand Members

        /// <summary>
        /// Defines the method that determines whether the command can execute in its current state.
        /// </summary>
        /// <param name="parameter">Data used by the command.  If the command does not require data to be passed, this object can be set to null.</param>
        /// <returns>
        /// true if this command can be executed; otherwise, false.
        /// </returns>
        bool ICommand.CanExecute(object parameter)
        {
            return canExecute;
        }

        /// <summary>
        /// Defines the method to be called when the command is invoked.
        /// </summary>
        /// <param name="parameter">Data used by the command.  If the command does not require data to be passed, this object can be set to null.</param>
        void ICommand.Execute(object parameter)
        {
            this.DoExecute(parameter);

        }

        #endregion


        /// <summary>
        /// Occurs when can execute is changed.
        /// </summary>
        public event EventHandler CanExecuteChanged;

        /// <summary>
        /// Occurs when the command is about to execute.
        /// </summary>
        public event CancelCommandEventHandler Executing;

        /// <summary>
        /// Occurs when the command executed.
        /// </summary>
        public event CommandEventHandler Executed;
    }
        

    /// <summary>
    /// The AsynchronousSCommand is a sCommand that runs on a thread from the thread pool.
    /// </summary>
    public class AsynchronousSCommand : sCommand, INotifyPropertyChanged
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AsynchronousSCommand"/> class.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="canExecute">if set to <c>true</c> the command can execute.</param>
        public AsynchronousSCommand(Action action, bool canExecute = true)
            : base(action, canExecute)
        {
            //  Initialise the command.
            Initialise();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AsynchronousSCommand"/> class.
        /// </summary>
        /// <param name="parameterizedAction">The parameterized action.</param>
        /// <param name="canExecute">if set to <c>true</c> [can execute].</param>
        public AsynchronousSCommand(Action<object> parameterizedAction, bool canExecute = true)
            : base(parameterizedAction, canExecute)
        {

            //  Initialise the command.
            Initialise();
        }

        /// <summary>
        /// Initialises this instance.
        /// </summary>
        private void Initialise()
        {
            //  Construct the cancel command.
            _cancelSCommand = new sCommand(
              () =>
              {
                  //  Set the Is Cancellation Requested flag.
                  IsCancellationRequested = true;
              }, true);
        }

        /// <summary>
        /// Executes the command.
        /// </summary>
        /// <param name="param">The param.</param>
        public override void DoExecute(object param)
        {
            //  If we are already executing, do not continue.
            if (IsExecuting)
                return;

            //  Invoke the executing command, allowing the command to be cancelled.
            CancelCommandEventArgs args = new CancelCommandEventArgs() { Parameter = param, Cancel = false };
            InvokeExecuting(args);

            //  If the event has been cancelled, bail now.
            if (args.Cancel)
                return;

            //  We are executing.
            IsExecuting = true;

            //  Store the calling dispatcher.
            callingDispatcher = Dispatcher.CurrentDispatcher;

            // Run the action on a new thread from the thread pool (this will therefore work in SL and WP7 as well).
            ThreadPool.QueueUserWorkItem(
              (state) =>
              {
                  //  Invoke the action.
                  InvokeAction(param);

                  //  Fire the executed event and set the executing state.
                  ReportProgress(
                    () =>
                    {
                        //  We are no longer executing.
                        IsExecuting = false;

                        //  If we were cancelled, invoke the cancelled event - otherwise invoke executed.
                        if (IsCancellationRequested)
                            InvokeCancelled(new CommandEventArgs() { Parameter = param });
                        else
                            InvokeExecuted(new CommandEventArgs() { Parameter = param });

                        //  We are no longer requesting cancellation.
                        IsCancellationRequested = false;
                    }
                  );
              }
            );
        }

        /// <summary>
        /// Raises the property changed event.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        private void NotifyPropertyChanged(string propertyName)
        {
            //  Store the event handler - in case it changes between
            //  the line to check it and the line to fire it.
            PropertyChangedEventHandler propertyChanged = PropertyChanged;

            //  If the event has been subscribed to, fire it.
            if (propertyChanged != null)
                propertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Reports progress on the thread which invoked the command.
        /// </summary>
        /// <param name="action">The action.</param>
        public void ReportProgress(Action action)
        {
            if (IsExecuting)
            {
                if (callingDispatcher.CheckAccess())
                    action();
                else
                    callingDispatcher.BeginInvoke(((Action)(() => { action(); })));
            }
        }

        /// <summary>
        /// Cancels the command if requested.
        /// </summary>
        /// <returns>True if the command has been cancelled and we must return.</returns>
        public bool CancelIfRequested()
        {
            //  If we haven't requested cancellation, there's nothing to do.
            if (IsCancellationRequested == false)
                return false;

            //  We're done.
            return true;
        }

        /// <summary>
        /// Invokes the cancelled event.
        /// </summary>
        // <param name="args">The <see cref="Apex.MVVM.CommandEventArgs"/> instance containing the event data.</param>
        protected void InvokeCancelled(CommandEventArgs args)
        {
            CommandEventHandler cancelled = Cancelled;

            //  Call the cancelled event.
            if (cancelled != null)
                cancelled(this, args);
        }

        /// <summary>
        /// 
        /// </summary>
        protected Dispatcher callingDispatcher;

        /// <summary>
        /// Flag indicating that the command is executing.
        /// </summary>
        private bool isExecuting = false;

        /// <summary>
        /// Flag indicated that cancellation has been requested.
        /// </summary>
        private bool isCancellationRequested;

        /// <summary>
        /// The cancel command.
        /// </summary>
        private sCommand _cancelSCommand;

        /// <summary>
        /// The property changed event.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Occurs when the command is cancelled.
        /// </summary>
        public event CommandEventHandler Cancelled;

        /// <summary>
        /// Gets or sets a value indicating whether this instance is executing.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is executing; otherwise, <c>false</c>.
        /// </value>
        public bool IsExecuting
        {
            get
            {
                return isExecuting;
            }
            set
            {
                if (isExecuting != value)
                {
                    isExecuting = value;
                    NotifyPropertyChanged("IsExecuting");
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is cancellation requested.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is cancellation requested; otherwise, <c>false</c>.
        /// </value>
        public bool IsCancellationRequested
        {
            get
            {
                return isCancellationRequested;
            }
            set
            {
                if (isCancellationRequested != value)
                {
                    isCancellationRequested = value;
                    NotifyPropertyChanged("IsCancellationRequested");
                }
            }
        }

        /// <summary>
        /// Gets the cancel command.
        /// </summary>
        public sCommand CancelSCommand
        {
            get { return _cancelSCommand; }
        }

    }


    //-------------------------------------------------------------------------------------------
    // вспомогательные классы

    /// <summary>
    /// The CommandEventHandler delegate.
    /// </summary>
    public delegate void CommandEventHandler(object sender, CommandEventArgs args);

    /// <summary>
    /// The CancelCommandEvent delegate.
    /// </summary>
    public delegate void CancelCommandEventHandler(object sender, CancelCommandEventArgs args);

    /// <summary>
    /// CommandEventArgs - simply holds the command parameter.
    /// </summary>
    public class CommandEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the parameter.
        /// </summary>
        /// <value>The parameter.</value>
        public object Parameter { get; set; }
    }

    /// <summary>
    /// CancelCommandEventArgs - just like above but allows the event to 
    /// be cancelled.
    /// </summary>
    public class CancelCommandEventArgs : CommandEventArgs
    {
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="CancelCommandEventArgs"/> command should be cancelled.
        /// </summary>
        /// <value><c>true</c> if cancel; otherwise, <c>false</c>.</value>
        public bool Cancel { get; set; }
    }


    //--------------------------------------------------------------------------------
    // просто попытки
    /*
   public class TextChangedCommand : ICommand
   {
       public event EventHandler CanExecuteChanged;

       public void Execute(object parameter)
       {
           System.Windows.MessageBox.Show("Text Changed");
       }

       public bool CanExecute(object parameter)
       {
           return true;
       }
   }

  

   public class RelayCommands
   {
                
       
       // обработка команды Open
       private void Command_OpenXMLFile(object sender, ExecutedRoutedEventArgs e)
       {

           if (e.Parameter == null) return;

           int prm = 0;
           Int32.TryParse(e.Parameter.ToString(), out prm);

           OpenFileDialog dlg = new OpenFileDialog();
           dlg.CheckFileExists = true;

           // выберем в зависимости от кнопки действие
           switch (prm)
           {
               // загрузим XML файл с маршрутом носителя в соответствующую таблицу
               case 1:
                   dlg.Filter = "Маршрут носителя (*.xml)|*.xml";
                   break;

               // загрузим XML файл со списком целей в соответствующую таблицу
               case 2:
                   dlg.Filter = "Список целей с маршрутами (*.xml)|*.xml";
                   break;                
           }

           // если выбран параметр открытия папки с файлами
           if (prm == 3)
           {
               // выбор папки, содержащей нужные файлы для загрузки

           }
           else
           {
               // экземпляр XML парсера
               XMLRouteParser xmlOpen = new XMLRouteParser();

               // если файл выбрна и существует
                
               //if ((bool)dlg.ShowDialog(this))
               //{
               //    // то исходя из типа файла выберем тип парсинга и загрузим список
               //    switch (prm)
               //    {
               //        // загрузим XML файл с маршрутом носителя в соответствующую таблицу
               //        case 1:
               //            xmlOpen.LoadObjectRoute(dlg.FileName);
               //            break;

               //        // загрузим XML файл со списком целей в соответствующую таблицу
               //        case 2:
               //            xmlOpen.LoadTargetsList(dlg.FileName);
               //            break;
               //    }
               //}
                

           }
       }

       // обработка команды save
       private void Command_SaveXMLFile(object sender, ExecutedRoutedEventArgs e)
       {
           if (e.Parameter == null) return;

           // экземпляр диалога сохранения файлов
           SaveFileDialog dlg = new SaveFileDialog();
           int prm = 0;
           Int32.TryParse(e.Parameter.ToString(), out prm);
           // выберем в зависимости от кнопки действие
           switch (prm)
           {
               // сохраним в XML файл маршрут носителя из соответствующей таблицы
               case 1:
                   dlg.Filter = "Маршрут носителя (*.xml)|*.xml";
                   break;

               // сохраним в XML файл список целей из соответствующей таблицы
               case 2:
                   dlg.Filter = "Список целей с маршрутами (*.xml)|*.xml";
                   break;
           }            
           
           //dlg.Filter = "Ink Serialized Format (*.isf)|*.isf|" +
           //             "XAML Drawing File (*.xml)|*.xml|" +
           //             "All files (*.*)|*.*";
           

           // экземпляр XML парсера
           XMLRouteParser xmlSave = new XMLRouteParser();

           if (prm == 3)
           {
               
           }

            
           //if ((bool)dlg.ShowDialog(this))
           //{
                
           //      выберем в зависимости от кнопки действие
           //     switch (prm)
           //     {
           //         сохраним в XML файл маршрут носителя из соответствующей таблицы
           //        case 1:
           //            xmlSave.SaveObjectRoute(dlg.FileName,);
           //            break;

           //         сохраним в XML файл список целей из соответствующей таблицы
           //        case 2:
           //            xmlSave.SaveTargetsList(dlg.FileName,);
           //            break;


           //     }

                
           //    try{

           //        FileStream file = new FileStream(dlg.FileName,
           //                                FileMode.Create, FileAccess.Write);

           //         код сохранения списков в xml файл

           //        XamlWriter.Save(drawgrp, file);
                    
           //        file.Close();
           //    }
           //    catch (Exception exc)
           //    {
           //        MessageBox.Show(exc.Message, Title);
           //    }
                
           //}           

       }       

   }

   public class CommandBase : ICommand    
    {
        private Func<object, bool> _canExecute;        
        private Action<object> _executeAction; 
        private bool canExecuteCache;     

        public CommandBase(Action<object> executeAction, Func<object, bool> canExecute)  
        {           
            this._executeAction = executeAction;        
            this._canExecute = canExecute;    
        }        

        #region ICommand Members      

        public bool CanExecute(object parameter)      
        {          
            bool tempCanExecute = _canExecute(parameter);  
            canExecuteCache = tempCanExecute;          
            return canExecuteCache;      
        }     
         
        private event EventHandler _canExecuteChanged;
        public event EventHandler CanExecuteChanged   
        {          
            add { this._canExecuteChanged += value; } 
            remove { this._canExecuteChanged -= value; }  
        }    
         
        protected virtual void OnCanExecuteChanged()
        {          
            if (this._canExecuteChanged != null)    
                this._canExecuteChanged(this, EventArgs.Empty);   
        }        public void Execute(object parameter)   
        {         
            _executeAction(parameter);  
        }    
        #endregion  
    }

   */
    
   

}
