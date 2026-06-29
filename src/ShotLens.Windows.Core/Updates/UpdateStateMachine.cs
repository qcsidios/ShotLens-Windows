namespace ShotLens.Windows.Core.Updates;

public enum UpdateStateKind
{
    Idle,
    Checking,
    UpToDate,
    Available,
    Downloading,
    LaunchingInstaller,
    Failed
}

public sealed record UpdateState(
    UpdateStateKind Kind,
    AvailableUpdate? Update = null,
    double? Progress = null,
    string? Message = null);

public sealed class UpdateStateMachine
{
    public UpdateState State { get; private set; } = new(UpdateStateKind.Idle);

    public bool TryBeginCheck()
    {
        if (State.Kind is UpdateStateKind.Checking
            or UpdateStateKind.Downloading
            or UpdateStateKind.LaunchingInstaller)
        {
            return false;
        }

        State = new UpdateState(UpdateStateKind.Checking);
        return true;
    }

    public void SetUpToDate()
    {
        Require(UpdateStateKind.Checking);
        State = new UpdateState(UpdateStateKind.UpToDate);
    }

    public void SetAvailable(AvailableUpdate update)
    {
        Require(UpdateStateKind.Checking);
        State = new UpdateState(UpdateStateKind.Available, update);
    }

    public bool TryBeginDownload()
    {
        if (State is not { Kind: UpdateStateKind.Available, Update: { } update })
        {
            return false;
        }

        State = new UpdateState(UpdateStateKind.Downloading, update, 0);
        return true;
    }

    public void SetDownloadProgress(double progress)
    {
        Require(UpdateStateKind.Downloading);
        State = State with { Progress = Math.Clamp(progress, 0, 1) };
    }

    public void SetLaunchingInstaller()
    {
        Require(UpdateStateKind.Downloading);
        State = new UpdateState(UpdateStateKind.LaunchingInstaller, State.Update, 1);
    }

    public void SetFailed(string message) =>
        State = new UpdateState(UpdateStateKind.Failed, Message: message);

    private void Require(UpdateStateKind expected)
    {
        if (State.Kind != expected)
        {
            throw new InvalidOperationException(
                $"更新状态应为 {expected}，实际为 {State.Kind}。");
        }
    }
}
