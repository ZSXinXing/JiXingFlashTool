using FFmpeg.AutoGen;
using JiXingFlashTool.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace JiXingFlashTool.Utils
{
    public unsafe class VideoStreamDecoder
    {
        private readonly AVCodec* _aVCodec;
        private readonly AVCodecContext* _codecCtx;
        private readonly AVCodecParserContext* _parser;

        public VideoStreamDecoder()
        {

            ffmpeg.RootPath = StaticConstant.FFmpegPath;

            _aVCodec = ffmpeg.avcodec_find_decoder(AVCodecID.AV_CODEC_ID_H264);
            if (_aVCodec == null)
            {
                return;
            }
            _codecCtx = ffmpeg.avcodec_alloc_context3(_aVCodec);
            if (_codecCtx == null)
            {
                return;
            }
            if (ffmpeg.avcodec_open2(_codecCtx, _aVCodec, null) < 0)
            {
                ffmpeg.avcodec_close(_codecCtx);
                return;
            }
            _parser = ffmpeg.av_parser_init((int)AVCodecID.AV_CODEC_ID_H264);
            if (_parser == null)
            {
                ffmpeg.avcodec_close(_codecCtx);
                return;
            }
        }

        public unsafe void Decode(byte[] buffer, Action<YuvModel> action)
        {
            try
            {
                AVPacket* packet = ffmpeg.av_packet_alloc();
                ffmpeg.av_new_packet(packet, buffer.Length);
                GCHandle intPtr = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                packet->data = (byte*)intPtr.AddrOfPinnedObject();
                packet->size = buffer.Length;

                AVFrame* frame = ffmpeg.av_frame_alloc();

                ffmpeg.avcodec_send_packet(_codecCtx, packet);
                ffmpeg.avcodec_receive_frame(_codecCtx, frame);
                if (frame->data[0] != null)
                {
                    IntPtr[] dataIntPtr = new IntPtr[3];
                    int[] lineSize = new int[3];

                    dataIntPtr[0] = (IntPtr)frame->data[0];
                    dataIntPtr[1] = (IntPtr)frame->data[2];
                    dataIntPtr[2] = (IntPtr)frame->data[1];

                    lineSize[0] = frame->linesize[0];
                    lineSize[1] = frame->linesize[2];
                    lineSize[2] = frame->linesize[1];

                    action(new YuvModel(dataIntPtr, lineSize, frame->width, frame->height));
                }
                else
                {
                    action(null);
                }
                intPtr.Free();
                ffmpeg.av_packet_unref(packet);
                ffmpeg.av_packet_free(&packet);
                ffmpeg.av_frame_unref(frame);
                ffmpeg.av_free(frame);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }
        public void Free()
        {
            ffmpeg.avcodec_close(_codecCtx);
        }
    }
}
