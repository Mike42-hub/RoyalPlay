using CommunityToolkit.Mvvm.Input;
using RoyalPlay.Models;

namespace RoyalPlay.PageModels
{
    public interface IProjectTaskPageModel
    {
        IAsyncRelayCommand<ProjectTask> NavigateToTaskCommand { get; }
        bool IsBusy { get; }
    }
}