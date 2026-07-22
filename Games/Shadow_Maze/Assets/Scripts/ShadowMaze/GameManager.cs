using System.Collections;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace ShadowMaze
{
    /// <summary>
    /// Central game brain. Runs detection each frame, enforces the lose condition (turn light
    /// red, play clash, reset to the last barrel checkpoint) and the win condition (reaching the
    /// stairs shows the victory screen with the 3D reward). Also handles input and restart.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public enum State { Playing, Caught, Won }

        private PlayerController _player;
        private Searchlight[] _lights;
        private GameUI _ui;
        private ProceduralAudio _audio;
        private BeltShowcase _belt;

        private State _state = State.Playing;
        private int[] _startIndices;
        private int[] _startDirs;

        public void Initialize(PlayerController player, Searchlight[] lights, GameUI ui,
            ProceduralAudio audio, BeltShowcase belt, int[] startIndices, int[] startDirs)
        {
            _player = player;
            _lights = lights;
            _ui = ui;
            _audio = audio;
            _belt = belt;
            _startIndices = startIndices;
            _startDirs = startDirs;

            _ui.StepPressed += OnStepInput;
            _ui.RestartPressed += RestartGame;
            _player.ReachedStairs += OnReachedStairs;

            _state = State.Playing;
            UpdateStatus();
        }

        private void OnDestroy()
        {
            if (_ui != null)
            {
                _ui.StepPressed -= OnStepInput;
                _ui.RestartPressed -= RestartGame;
            }
            if (_player != null) _player.ReachedStairs -= OnReachedStairs;
        }

        private void Update()
        {
            if (_player == null) return; // not initialized (e.g. a leftover preview instance)
            if (_state != State.Playing) return;

            if (WasStepKeyPressed()) OnStepInput();

            CheckDetection();
        }

        private void OnStepInput()
        {
            if (_state != State.Playing) return;
            if (_player.StepForward())
            {
                _audio.PlayStep();
                UpdateStatus();
            }
        }

        /// <summary>
        /// Requirement 2 & 3: if any light shares Auris's cell and he is NOT on a barrel,
        /// he is detected.
        /// </summary>
        private void CheckDetection()
        {
            if (_state != State.Playing) return;
            if (_player.IsOnSafeZone) return;

            foreach (var light in _lights)
            {
                if (light.CurrentCell == _player.CurrentCell)
                {
                    StartCoroutine(CaughtRoutine(light));
                    return;
                }
            }
        }

        private IEnumerator CaughtRoutine(Searchlight light)
        {
            _state = State.Caught;

            light.SetAlert(true);          // light turns red instantly
            _audio.PlayClash();            // sabre clash placeholder
            _ui.FlashAlert();
            _ui.SetStatus("Викрито! Брязкіт шабель…", new Color(1f, 0.4f, 0.35f));

            yield return new WaitForSeconds(0.7f);

            light.SetAlert(false);
            _player.ResetToCheckpoint();   // back to last barrel checkpoint

            _state = State.Playing;
            UpdateStatus();
        }

        private void OnReachedStairs()
        {
            if (_state == State.Won) return;
            StartCoroutine(WinRoutine());
        }

        private IEnumerator WinRoutine()
        {
            _state = State.Won;
            _ui.SetStatus("Ауріс досяг Сходів!", new Color(0.6f, 1f, 0.6f));
            yield return new WaitForSeconds(0.35f);

            _audio.PlayVictory();
            _belt.SetSpinning(true);
            _ui.ShowVictory(_belt.Texture);
        }

        public void RestartGame()
        {
            StopAllCoroutines();
            _ui.HideVictory();
            _belt.SetSpinning(false);

            _player.ResetToStart();
            for (int i = 0; i < _lights.Length; i++)
                _lights[i].ResetPatrol(_startIndices[i], _startDirs[i]);

            _state = State.Playing;
            UpdateStatus();
        }

        private void UpdateStatus()
        {
            if (_state != State.Playing) return;
            if (_player.IsOnSafeZone)
                _ui.SetStatus("У безпеці за бочкою. Чекай на просвіт!", new Color(0.6f, 1f, 0.7f));
            else
                _ui.SetStatus("Обережно — стережися променів!", new Color(1f, 0.95f, 0.7f));
        }

        private bool WasStepKeyPressed()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame
                || kb.spaceKey.wasPressedThisFrame))
                return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)
                || Input.GetKeyDown(KeyCode.Space))
                return true;
#endif
            return false;
        }
    }
}
