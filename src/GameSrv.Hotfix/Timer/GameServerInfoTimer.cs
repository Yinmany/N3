namespace N3;

[Invokable(InvokeId.GameServerInfoTimer)]
public class GameServerInfoTimer : ATimer<ServerApp>
{
    private int num = 0;

    protected override void On(TimerInfo timer, ServerApp app)
    {

    }
}