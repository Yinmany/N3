namespace N3.Event
{
    [Invokable(InvokeId.NetDisConnect)]
    class OnNetDisConnect : AInvokable<NetSession>
    {
        public override void On(NetSession session)
        {
            if (session.RoleId == 0)
                return;

        }
    }
}