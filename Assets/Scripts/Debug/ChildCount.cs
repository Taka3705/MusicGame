using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChildCount : MonoBehaviour {
	[SerializeField]
	private int num;

	// Update is called once per frame
	void Update () {
		num = this.transform.childCount;
	}
}
