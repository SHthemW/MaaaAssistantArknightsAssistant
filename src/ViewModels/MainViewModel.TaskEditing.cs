using CommunityToolkit.Mvvm.Input;

namespace Game_Daily_Routine_Launcher;

public partial class MainViewModel
{
    private const string CustomTaskIdPrefix = "custom";

    [RelayCommand]
    private void AddTask()
    {
        if (!CanEditTasks)
            return;

        var nextNumber = Tasks.Count + 1;
        var task = new GameTaskConfig
        {
            Id = CreateTaskId(),
            Name = $"新任务 {nextNumber}",
            Enabled = true,
            TimeoutMinutes = 60
        };

        var vm = new GameTaskViewModel(task)
        {
            ConfigChanged = SaveConfig,
            IsConfigExpanded = true
        };

        Tasks.Add(vm);
        SaveConfig();
        AddLog($"已新增任务：{vm.Name}");
    }

    public void RemoveTask(GameTaskViewModel? taskVm)
    {
        if (taskVm == null || !CanEditTasks)
            return;

        if (!Tasks.Remove(taskVm))
            return;

        SaveConfig();
        AddLog($"已删除任务：{taskVm.Name}");
    }

    private string CreateTaskId()
    {
        var existingIds = Tasks.Select(t => t.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        string id;
        do
        {
            id = $"{CustomTaskIdPrefix}-{DateTime.Now:yyyyMMddHHmmssfff}-{Random.Shared.Next(1000, 9999)}";
        }
        while (existingIds.Contains(id));

        return id;
    }
}
