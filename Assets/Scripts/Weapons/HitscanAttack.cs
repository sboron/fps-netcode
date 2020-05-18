﻿using Ice;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
[RequireComponent(typeof(Lifetime))]
public class HitscanAttack : MonoBehaviour {
  public AnimationCurve fadeCurve;

  private LineRenderer lr;
  private Lifetime lt;
  private Color color;

  private void Start() {
    lr = GetComponent<LineRenderer>();
    lt = GetComponent<Lifetime>();
    color = lr.material.color;
  }

  private void Update() {
    var theta = lt.TimeLeft / lt.lifetime;
    var alpha = fadeCurve.Evaluate(theta);
    color.a = alpha;
    lr.material.color = color;
  }

  public GameObject CheckHit() {
    int mask = LayerMask.NameToLayer("Player");
    RaycastHit hit;
    if (Physics.Raycast(transform.position, transform.forward, out hit, float.MaxValue, mask)) {
      Debug.Log($"On our end, we hit {hit.collider.gameObject.name}");
      return hit.collider.gameObject;
    }
    return null;
  }
}
