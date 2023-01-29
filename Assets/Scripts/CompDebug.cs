using System.Linq;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CompScreen))]
public sealed class CompDebug : MonoBehaviour
{
	private Text _viewVars;
	private Text _viewLog;

	private void Awake()
	{
		_viewVars = GetComponentsInChildren<Text>().FirstOrDefault(_ => _.name.Contains("vars"));
		_viewLog = GetComponentsInChildren<Text>().FirstOrDefault(_ => _.name.Contains("log"));
	}

	private void LateUpdate()
	{
		_viewVars.text = DataShared.I.RenderVars();
		_viewLog.text = DataShared.I.RenderLog();
	}
}
