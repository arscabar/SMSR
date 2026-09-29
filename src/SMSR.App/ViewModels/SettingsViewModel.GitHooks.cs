using SMSR.App.Services;

namespace SMSR.App.ViewModels;

public sealed partial class SettingsViewModel
{
    private bool _gitHookInstalled;
    public bool GitHookInstalled { get => _gitHookInstalled; private set => SetField(ref _gitHookInstalled, value); }
    public string GitHookStatus => GitHookInstalled ? "전역 Git post-commit 훅 등록됨" : "전역 Git 훅 미설치";

    private bool ReadGitHookState()
    {
        try { return GitAutoIndexHook.IsRegistered(); }
        catch { return false; }
    }

    private void InstallGitHook()
    {
        if (!_platform.Confirm("전역 Git 훅 설치",
                "모든 Git 저장소의 post-commit에서 SMSR이 실행됩니다. 전역 core.hooksPath가 저장소별 .git/hooks 실행을 우회할 수 있습니다. 계속할까요?")) return;
        try
        {
            var backup = GitAutoIndexHook.Register(Environment.ProcessPath
                ?? throw new InvalidOperationException("실행 파일 경로를 확인할 수 없습니다."));
            GitHookInstalled = ReadGitHookState();
            OnPropertyChanged(nameof(GitHookStatus));
            StatusMessage = backup is null ? "전역 Git 훅을 등록했습니다."
                : $"전역 Git 훅을 등록했습니다. 기존 설정 백업: {backup}";
        }
        catch (Exception error) { StatusMessage = "전역 Git 훅 설치 실패: " + error.Message; }
    }

    private void RemoveGitHook()
    {
        if (!_platform.Confirm("전역 Git 훅 해제", "SMSR이 등록한 전역 Git 훅 설정만 해제합니다. 계속할까요?")) return;
        try
        {
            GitAutoIndexHook.Unregister();
            GitHookInstalled = ReadGitHookState();
            OnPropertyChanged(nameof(GitHookStatus));
            StatusMessage = "전역 Git 훅을 해제했습니다.";
        }
        catch (Exception error) { StatusMessage = "전역 Git 훅 해제 실패: " + error.Message; }
    }
}
