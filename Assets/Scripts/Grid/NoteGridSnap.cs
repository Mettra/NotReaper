﻿using System.Collections;
using System.Collections.Generic;
using NotReaper.Targets;
using UnityEngine;
using UnityEngine.EventSystems;

namespace NotReaper.Grid {


    public class NoteGridSnap : MonoBehaviour {

        public enum SnappingMode { None, Grid, Melee }

        [SerializeField] private float xSize;
        [SerializeField] private float ySize;

        private Vector2 gridOffset;

        static SnappingMode snapMode = SnappingMode.Grid;
        bool hover = false;

        public Transform ghost;
        public LayerMask notesLayer;
        public GameObject standardGrid;
        public GameObject meleeGrid;
        public Timeline timeline;


        private void Awake() {
            SetSnappingMode(SnappingMode.Grid);
            gridOffset = transform.position;
        }

        private void Update() {
            if (hover) {
                if (EventSystem.current.IsPointerOverGameObject())
                    return;

                var mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                //Vector2 snapPos = SnapToGrid(mousePos);
                //var ghostPos = ghost.position;
                //ghostPos.x = snapPos.x;
                //ghostPos.y = snapPos.y;
                ghost.position = SnapToGrid(mousePos);

                if (Input.GetMouseButtonDown(0)) {
                    if (EventSystem.current.IsPointerOverGameObject())
                        return;

                    timeline.AddTarget(ghost.position.x, ghost.position.y);
                }
            }

            if (Input.GetMouseButtonDown(1)) {
                if (EventSystem.current.IsPointerOverGameObject())
                    return;

                timeline.DeleteTarget(NoteUnderMouse());
            }

            HandleKeybinds();

        }

        private void HandleKeybinds() {
            if (Input.GetKeyDown(KeyCode.G)) {
                SetSnappingMode(SnappingMode.Grid);
            }
            if (Input.GetKeyDown(KeyCode.M)) {
                SetSnappingMode(SnappingMode.Melee);
            }
            if (Input.GetKeyDown(KeyCode.N)) {
                SetSnappingMode(SnappingMode.None);
            }

        }

        private Target NoteUnderMouse() {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            ray.origin = new Vector3(ray.origin.x, ray.origin.y, -1f);
            ray.direction = Vector3.forward;
            Debug.DrawRay(ray.origin, ray.direction);
            if (Physics.Raycast(ray, out hit, 2, notesLayer)) {
                Transform objectHit = hit.transform;

                Target target = objectHit.GetComponent<Target>().gridTarget;


                return target;
            }
            return null;
        }


        private void OnMouseEnter() {
            hover = true;
            ghost.gameObject.SetActive(true);
        }

        private void OnMouseExit() {
            hover = false;
            ghost.gameObject.SetActive(false);
        }

        public void SetSnappingMode(SnappingMode snappingMode) {
            snapMode = snappingMode;
            meleeGrid.SetActive(snapMode == SnappingMode.Melee);
            standardGrid.SetActive(snapMode == SnappingMode.Grid);
        }


        public Vector2 GetNearestPointOnGrid(Vector2 pos) {

            //pos -= gridOffset; //Enable if grid is actually offset.
            pos.y += 0.45f;
            int x = Mathf.FloorToInt(pos.x / xSize);
            int y = Mathf.FloorToInt(pos.y / ySize);

            Vector2 result = new Vector2((float) x * xSize, (float) y * ySize);
            result.x += 0.65f;

            //result += gridOffset; //Enable if grid is actually offset.

            return result;
        }

        public Vector3 SnapToGrid(Vector3 pos) {
            switch (snapMode) {
                case SnappingMode.Grid:
                    return GetNearestPointOnGrid(pos);
                case SnappingMode.Melee:
                    return new Vector3(Mathf.Sign(pos.x) * 2, Mathf.Sign(pos.y), pos.z);
            }
            return pos;
        }


    }


}