GameObject actionScene = GameObject.Find("ActionScene");
RG.Scene.ActionScene _actionScene = actionScene.GetComponent<RG.Scene.ActionScene>();

Log(_actionScene._femaleActors.Count);

if (_actionScene._femaleActors.Count==0)
{
	Log("No Girls");
    return;
}

RG.Scene.Action.Core.Actor girl = _actionScene._femaleActors[0];
RG.User.Status girlStatus = girl._status;

string girlName = girlStatus.FullName;
Log(girlName);

girlStatus.Parameters[0]=80;
Log(girlStatus.Parameters[0]);



