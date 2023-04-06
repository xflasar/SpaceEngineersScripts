// Combat Manager Script
// Created by xSupeFly
// Discord: xSupeFly#2911

IEnumerator<bool> _stateMachine;

Program()
{
    _stateMachine = RunStuffOverTime();
    Runtime.UpdateFrequency |= UpdateFrequency.Once;
}

void Main(string argument, UpdateType updateSource)
{
    TimeSinceFirstRun += Runtime.TimeSinceLastRun;
    if ((updateType & UpdateType.Once) == UpdateType.Once)
    {
        RunStateMachine();
    }
}

public void RunStateMachine()
{
    if (_stateMachine != null) 
    {
        bool hasMoreSteps = _stateMachine.MoveNext();

        if (hasMoreSteps)
        {
            Runtime.UpdateFrequency |= UpdateFrequency.Once;
        } 
        else 
        {
            _stateMachine.Dispose();
            _stateMachine = null;
        }
    }
}

public IEnumerator<bool> RunStuffOverTime() 
{
    yield return true;

    int counter = 0;

    while (true) 
    {
        Echo("Performance (Ms): " + Runtime.LastRunTimeMs);
        logger.GetLoggerLog().ForEach(l => Echo(l));
        if(500 > counter){
            counter++;
        }else
        {
            counter = 0;
            RunStuffOverTimeT();
        }

        yield return true;
    }
}