using System;
using System.Collections;
using HTC.UnityPlugin.Multimedia;
using UnityEngine;

namespace Innoactive.Hub.Media.Streaming
{
    /// <summary>
    /// Implementation of <see cref="IVideoStreamPlayer"/> for the Vive Media Decoder plugin.
    /// </summary>
    [RequireComponent(typeof(ViveMediaDecoder))]
    public class ViveMediaDecoderStreamPlayer : MonoBehaviour, IVideoStreamPlayer
    {
        /// <summary>
        /// <see cref="ViveMediaDecoder"/> instance to use.
        /// </summary>
        protected ViveMediaDecoder MediaDecoder;

        [SerializeField]
        [Tooltip("Interval in seconds between playback attempts for an inactive player.")]
        protected float PollingIntervalSeconds = 5;

        protected WaitForSeconds PollingInterval;

        /// <inheritdoc />
        public event EventHandler<AspectRatioChangedEventArgs> AspectRatioChanged;

        /// <inheritdoc />
        public void SetStreamUrl(string url)
        {
            MediaDecoder.initDecoder(url);
            MediaDecoder.onInitComplete.AddListener(() => EmitAspectRatioChanged(GetAspectRatio()));
        }

        /// <inheritdoc />
        public void Play()
        {
            ViveMediaDecoder.DecoderState state = MediaDecoder.getDecoderState();

            if (state == ViveMediaDecoder.DecoderState.INITIALIZED ||
                state == ViveMediaDecoder.DecoderState.PAUSE ||
                state == ViveMediaDecoder.DecoderState.STOP ||
                state == ViveMediaDecoder.DecoderState.EOF)
            {
                StartDecoding();
            }
            else
            {
                MediaDecoder.onInitComplete.AddListener(StartDecoding);
            }
        }

        /// <inheritdoc />
        public void Stop()
        {
            MediaDecoder.stopDecoding();
        }

        /// <summary>
        /// Returns aspect ratio as a float.
        /// </summary>
        protected virtual float GetAspectRatio()
        {
            int width = 0;
            int height = 0;
            MediaDecoder.getVideoResolution(ref width, ref height);

            if (width > 0 && height > 0)
            {
                return (float)width / height;
            }
            else
            {
                return 1;
            }
        }

        protected virtual void Start()
        {
            MediaDecoder = GetComponent<ViveMediaDecoder>();
            PollingInterval = new WaitForSeconds(PollingIntervalSeconds);
        }

        protected virtual void EmitAspectRatioChanged(float aspectRatio)
        {
            if (AspectRatioChanged != null)
            {
                AspectRatioChanged.Invoke(this, new AspectRatioChangedEventArgs(aspectRatio));
            }
        }

        /// <summary>
        /// Keeps attempting to start playback until a valid resolution confirms the stream is being received.
        /// </summary>
        protected virtual IEnumerator CheckForActiveStream()
        {
            int width = 0;
            int height = 0;

            while (width <= 0 || height <= 0)
            {
                MediaDecoder.getVideoResolution(ref width, ref height);

                Play();

                yield return PollingInterval;
            }
        }

        /// <summary>
        /// Initiates stream playback.
        /// </summary>
        protected virtual void StartDecoding()
        {
            MediaDecoder.onInitComplete.RemoveAllListeners();
            MediaDecoder.startDecoding();
            StartCoroutine(CheckForActiveStream());
        }
    }
}
