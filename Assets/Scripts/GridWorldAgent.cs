﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;

public class GridWorldAgent : Agent {
    public GridArea gridArea;
    public Camera renderCamera;

    private EnvironmentParameters environmentParameters;
    private float stepReward, fireReward, targetReward; // Different rewards

    // An enum representation for possible actions
    private enum Actions : int {
        None = 0,
        Up = 1,
        Down = 2,
        Left = 3,
        Right = 4
    }

    /// <summary>
    /// Initializes the environment and sets values based on what is received
    /// from Python
    /// </summary>
    public override void Initialize() {
        environmentParameters = Academy.Instance.EnvironmentParameters;
        stepReward = environmentParameters.GetWithDefault("step_reward", -0.1f);
        targetReward = environmentParameters.GetWithDefault("target_reward", 1.0f);
        fireReward = environmentParameters.GetWithDefault("fire_reward", -1.0f);
        MaxStep = (int)environmentParameters.GetWithDefault("max_steps", 250f);
        Time.timeScale = environmentParameters.GetWithDefault("time_scale", 1.0f);
    }

    public override void OnEpisodeBegin() {
        gridArea.ResetArea();
    }

    /// <summary>
    /// Decides what the agent should do when an array of vectorAction is received.
    /// In this case there is just one element which represents the direction it should move.
    /// </summary>
    /// <param name="vectorAction">An array depicting which actions have what values</param>
    public override void OnActionReceived(float[] vectorAction) {
        Actions action = (Actions)Mathf.FloorToInt(vectorAction[0]);
        Vector3 targetLoc = transform.position;

        // Get target location
        switch(action) {
            case Actions.None:
                break;
            case Actions.Up:
                targetLoc += new Vector3(0f, 0f, 1f);
                break;
            case Actions.Down:
                targetLoc += new Vector3(0f, 0f, -1f);
                break;
            case Actions.Left:
                targetLoc += new Vector3(-1f, 0f, 0f);
                break;
            case Actions.Right:
                targetLoc += new Vector3(1f, 0f, 0f);
                break;
            default:
                Debug.LogAssertion("Invalid action received");
                break;
        }

        // Check to see if the target location overlaps either the "target", "fire", or "wall"
        Collider[] colliders = Physics.OverlapBox(targetLoc, new Vector3(0.4f, 0.4f, 0.4f));
        int wallCount = 0;
        int fireCount = 0;
        int targetCount = 0;
        foreach (Collider collider in colliders) {
            if (collider.CompareTag("wall")) wallCount++;
            else if (collider.CompareTag("target")) targetCount++;
            else if (collider.CompareTag("fire")) fireCount++;
        }

        // Each non-terminal step starts with this reward
        SetReward(stepReward);

        // If it's not a wall, then it's a valid move
        if (wallCount == 0) {
            transform.position = targetLoc;
            if (fireCount == 1) {
                SetReward(fireReward);
                EndEpisode();
            } else if (targetCount == 1) {
                SetReward(targetReward);
                EndEpisode();
            }
        }
    }

    /// <summary>
    /// This is used if a human is playing
    /// </summary>
    /// <param name="actionsOut">an array of actions that will be fed into OnActionReceived</param>
    public override void Heuristic(float[] actionsOut) {
        actionsOut[0] = (float)Actions.None;

        if (Input.GetKey(KeyCode.W))
            actionsOut[0] = (float)Actions.Up;
        if (Input.GetKey(KeyCode.S))
            actionsOut[0] = (float)Actions.Down;
        if (Input.GetKey(KeyCode.A))
            actionsOut[0] = (float)Actions.Left;
        if (Input.GetKey(KeyCode.D))
            actionsOut[0] = (float)Actions.Right;
    }

    public void FixedUpdate() {
        // Force render
        if (renderCamera != null) {
            renderCamera.Render();
        }
        RequestDecision();
    }
}
