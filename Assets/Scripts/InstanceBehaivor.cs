using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InstanceBehaivor : MonoBehaviour {

    public GameController gameController {
        get { return GameController.inst;}
    }
    public VisualController visualController {
        get { return VisualController.inst;}
    }
}
