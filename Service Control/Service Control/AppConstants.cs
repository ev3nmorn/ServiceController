using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service_Control
{
    class AppConstants
    {
        public const string AutoString = "Автоматически",
            ChangeResult = ": состояние службы изменено на ",
            ConfigExtension = "cfg",
            DefaultFileName = "services",
            DisabledCfg = "Disabled",
            DisabledString = "Отключена",
            EnabledCfg = "Enabled",
            Filter = "Config files (*.cfg)|*.cfg",
            InstructionsLine1 = "***  Возможные параметры: Остановлена, Выполняется  ***",
            InstructionsLine2 = "*** Для указания необходимости учета строки указать параметр Enabled, в противном случае - Disabled ***",
            ManuallyString = "Вручную",
            NoAnyChanges = "Никаких изменений не было внесено",
            NoFileSelected = "Укажите конфигурационный файл",
            NotSetString = "Не задано",
            PathToServices = @"SYSTEM\CurrentControlSet\Services\",
            RegularExprParams = @"(?<=\[)[^[]*(?=\])",
            RunningState = "Running",
            RunningStateCfg = "Выполняется",
            ServiceNotFound = " служба не найдена",
            ServicesStateField = "Start",
            ServicesWereWritten = "Список служб записан в ",
            StoppedState = "Stopped",
            StoppedStateCfg = "Остановлена",
            WrongConfLineFormat = ": неверный формат строки конфигурации",
            WrongStatus = ": неверный параметр состояния ",
            WrongEnabled = ": неверный параметр активации строки конфигурации ";

        public static readonly string BlockEnd = new string('-', 136);

        public const Int32 AutoValue = 2,
            DisabledValue = 4,
            ManuallyValue = 3,
            MaxIntendDisplayName = 100,
            MaxIntendServiceName = 70,
            ResultListSize = 6;
    }
}
