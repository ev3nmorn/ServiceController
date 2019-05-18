using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceProcess;
using System.IO;
using Microsoft.Win32;
using System.Text.RegularExpressions;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Service_Control
{
    class ServiceControlFunctions
    {
        [DllImport("SecurityLib.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern bool SetRegKeyAdvanced(string serviceName, int startMode);

        // Перевод состояния
        private static string GetServiceStatusCfg(string state)
        {
            string result = String.Empty;

            switch(state)
            {
                case AppConstants.RunningState:
                    result = AppConstants.RunningStateCfg;
                    break;

                case AppConstants.StoppedState:
                    result = AppConstants.StoppedStateCfg;
                    break;
            }

            return result;
        }

        // Получить список всех служб по формату конфигурационного файла
        public static List<string> GetAllServices()
        {
            List<string> result = new List<string>();

            foreach (ServiceController controller in ServiceController.GetServices())
            {
                string lol = controller.Status.ToString();
                result.Add(controller.DisplayName + 
                    (new string(' ', AppConstants.MaxIntendDisplayName - controller.DisplayName.Length)) +
                    '[' + controller.ServiceName + ']' +
                    (new string(' ', AppConstants.MaxIntendServiceName - controller.ServiceName.Length - 2)) +
                    '[' + GetServiceStatusCfg(controller.Status.ToString()) + ']' + 
                    '[' + AppConstants.DisabledCfg + ']');
            }

            return result;
        }

        // Получить текущее состояние службы
        private static string GetServiceStatus(string serviceName)
        {
            ServiceController _serviceController = new ServiceController(serviceName);
            string status = _serviceController.Status.ToString();
            _serviceController.Close();
            return status;
        }

        // Получить отображаемое имя по имени службы
        private static string GetDispNameByServName(string serviceName)
        {
            foreach (ServiceController controller in ServiceController.GetServices())
                if (controller.ServiceName == serviceName)
                    return controller.DisplayName;

            return string.Empty;
        }

        // Удалить комментарии из конфигурации (комментарии обосабливаются тройными звездочками)
        public static void DeleteComments(ref List<string> configs)
        {
            Regex reg = new Regex(@"(\*\*\*).*(\*\*\*)");
            for (int i = 0; i < configs.Count; ++i)
                if (reg.IsMatch(configs[i]))
                {
                    configs.RemoveAt(i);
                    --i;
                }
        }

        /* Инициализация вектора состояния выполнения конфигурации
         * [(0/1, измененное состояние, имя службы) (0/1, неверный формат строки)
         *  (0/1, служба не найдена) (0/1, неверный параметр состояния)
         *  (0/1, неверный тип необходимости строки конфигурации),
         *  (0/1, другие ошибки)]
         */
        private static List<KeyValuePair<int, string>> InitStatusList()
        {
            List<KeyValuePair<int, string>> status = new List<KeyValuePair<int, string>>();

            for (int i = 0; i < AppConstants.ResultListSize; ++i)
                status.Add(new KeyValuePair<int, string>(0, String.Empty));

            return status;
        }

        // Проверка корректности типа запуска в конфигурации
        private static bool CheckStatusCfg(string status)
        {
            bool result;

            switch (status)
            {
                case AppConstants.StoppedStateCfg:
                    result = true;
                    break;

                case AppConstants.RunningStateCfg:
                    result = true;
                    break;

                default:
                    result = false;
                    break;
            }

            return result;
        }

        // Проверка корректности параметра активации строки конфигурации
        private static bool CheckEnabledCfg(string enabled)
        {
            bool result;

            switch (enabled)
            {
                case AppConstants.DisabledCfg:
                    result = true;
                    break;

                case AppConstants.EnabledCfg:
                    result = true;
                    break;

                default:
                    result = false;
                    break;
            }

            return result;
        }

        // Получить коллекцию параметров
        private static MatchCollection GetMatchCollectionParams(string configLine)
        {
            Regex paramRegex = new Regex(AppConstants.RegularExprParams);
            MatchCollection parameters = paramRegex.Matches(configLine);

            return parameters;
        }

        // Убрать лишние пробелы
        private static string RemoveExcessSpaces(string line)
        {
            Regex reg = new Regex(@"\s+");
            line = reg.Replace(line, " ");
            return line;
        }

        // Проверка строки конфигураций на ошибки
        private static bool CheckConfigErrors(string configLine, ref List<KeyValuePair<int, string>> status)
        {
            MatchCollection parameters = GetMatchCollectionParams(configLine);

            if (parameters.Count != 3)
            {
                status[1] = new KeyValuePair<int, string>(1, RemoveExcessSpaces(configLine) +
                    AppConstants.WrongConfLineFormat);
                return false;
            }

            if (!CheckStatusCfg(parameters[1].ToString()))
            {
                    status[3] = new KeyValuePair<int, string>(1, parameters[0] +
                        AppConstants.WrongStatus + parameters[1].ToString());
                    return false;
            }

            if (!CheckEnabledCfg(parameters[2].ToString()))
            {
                status[4] = new KeyValuePair<int, string>(1, parameters[0] +
                    AppConstants.WrongEnabled + parameters[2].ToString());
                return false;
            }

            return true;
        }

        // Получить параметры конфигурационного файла в виде [имя службы, состояние, активация]
        private static List<string> GetParams(string configLine)
        {
            List<string> result = new List<string>();
            MatchCollection parameters = GetMatchCollectionParams(configLine);

            foreach (Match m in parameters)
                result.Add(m.ToString());

            return result;
        }

        // Проверка на существование ключа в реестре
        private static bool CheckExistKey(string serviceName, ref List<KeyValuePair<int, string>> status)
        {
            RegistryKey regkey = Registry.LocalMachine.OpenSubKey(AppConstants.PathToServices + serviceName);
            if (regkey == null)
            {
                status[2] = new KeyValuePair<int, string>(1, serviceName + AppConstants.ServiceNotFound);
                return false;
            }

            regkey.Close();
            return true;
        }

        private static bool StartService(string serviceName, ref List<KeyValuePair<int, string>> status)
        {
            try
            {
                ServiceController serviceController = new ServiceController(serviceName);
                if (!serviceController.ServiceHandle.IsInvalid)
                {
                    serviceController.Start();
                    serviceController.WaitForStatus(ServiceControllerStatus.Running);
                }
                serviceController.Close();
                return true;
            }
            catch(Exception ex) {
                status[5] = new KeyValuePair<int, string>(1, serviceName + " " + ex.Message);
                return false;
            }
        }

        private static bool StopService(string serviceName, ref List<KeyValuePair<int, string>> status)
        {
            try
            {
                ServiceController serviceController = new ServiceController(serviceName);
                if (!serviceController.ServiceHandle.IsInvalid)
                {
                    serviceController.Stop();
                    serviceController.WaitForStatus(ServiceControllerStatus.Stopped);
                }
                serviceController.Close();
                return true;
            }
            catch (Exception ex) {
                status[5] = new KeyValuePair<int, string>(1, serviceName + " " + ex.Message);
                return false;
            }
        }

        // Применить настройки
        public static List<KeyValuePair<int, string>> ApplySettings(string configLine)
        {
            List<KeyValuePair<int, string>> status = InitStatusList();
            List<string> cfgParams;
            RegistryKey regkey;
            bool changeState = true; 

            if (!CheckConfigErrors(configLine, ref status))
                return status;

            cfgParams = GetParams(configLine);

            if (cfgParams[2] == AppConstants.DisabledCfg)
                return status;

            if (!CheckExistKey(cfgParams[0], ref status))
                return status;

            if (GetServiceStatusCfg(GetServiceStatus(cfgParams[0])) == cfgParams[1])
                return status;

            try
            {
                regkey = Registry.LocalMachine.OpenSubKey(AppConstants.PathToServices + cfgParams[0], true);

                if (cfgParams[1] == AppConstants.StoppedStateCfg)
                {
                    regkey.SetValue(AppConstants.ServicesStateField, AppConstants.DisabledValue, RegistryValueKind.DWord);
                    changeState = StopService(cfgParams[0], ref status);
                }
                else
                {
                    regkey.SetValue(AppConstants.ServicesStateField, AppConstants.AutoValue, RegistryValueKind.DWord);
                    changeState = StartService(cfgParams[0], ref status);
                }

                regkey.Close();                             
            }
            catch (Exception)
            {
                if (cfgParams[1] == AppConstants.StoppedStateCfg)
                    changeState = SetRegKeyAdvanced(cfgParams[0], AppConstants.DisabledValue);
                else
                    changeState = SetRegKeyAdvanced(cfgParams[0], AppConstants.AutoValue);
            }
            finally
            {
                if (changeState)
                    status[0] = new KeyValuePair<int, string>(1, cfgParams[0] + " "
                        + GetDispNameByServName(cfgParams[0]) + AppConstants.ChangeResult + cfgParams[1]);
            }

            return status;
        }
    }
}
