using System.Linq;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Canvas))]
public sealed class CompDebug : ScreenBase
{
	private Text _viewVars;
	private Text _viewLog;

	private void Awake()
	{
		_viewVars = GetComponentsInChildren<Text>().First(_ => _.name.Contains("vars"));
		_viewLog = GetComponentsInChildren<Text>().First(_ => _.name.Contains("log"));

		DataShared.I.Log("start server with network panel");
		DataShared.I.Log("press Escape to terminate app");
		DataShared.I.Log("use WASD, mouse move and LMB to control");
		DataShared.I.Log("use CTRL, hold input");
	}

	private void LateUpdate()
	{
		_viewVars.text = DataShared.I.RenderVars();
		_viewLog.text = DataShared.I.RenderLog();
	}
}