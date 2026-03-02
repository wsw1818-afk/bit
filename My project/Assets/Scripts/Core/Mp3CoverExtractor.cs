using UnityEngine;
using System;
using System.IO;

namespace AIBeat.Core
{
    /// <summary>
    /// MP3 파일의 ID3v2 태그에서 APIC(앨범 아트) 프레임을 추출하여
    /// UnityEngine.Texture2D로 반환.
    ///
    /// 지원 형식: ID3v2.3 / ID3v2.4
    /// APIC 바이너리 구조:
    ///   [text_encoding: 1 byte]
    ///   [MIME type: null-terminated string, e.g. "image/jpeg\0"]
    ///   [picture_type: 1 byte] (3 = Cover, but we accept any)
    ///   [description: null-terminated string (1 or 2 bytes per null depending on encoding)]
    ///   [image_data: remaining bytes]
    /// </summary>
    public static class Mp3CoverExtractor
    {
        /// <summary>
        /// MP3 파일 경로에서 임베드된 커버 아트를 추출.
        /// 실패 시 null 반환.
        /// </summary>
        public static Texture2D ExtractCover(string mp3FilePath)
        {
            if (string.IsNullOrEmpty(mp3FilePath) || !File.Exists(mp3FilePath))
                return null;

            try
            {
                byte[] apicData = ReadApicFrame(mp3FilePath);
                if (apicData == null || apicData.Length < 4)
                    return null;

                byte[] imageBytes = ParseApicImageBytes(apicData);
                if (imageBytes == null || imageBytes.Length < 4)
                    return null;

                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (tex.LoadImage(imageBytes))
                {
#if UNITY_EDITOR
                    Debug.Log($"[Mp3Cover] 앨범 아트 추출 성공: {Path.GetFileName(mp3FilePath)} ({imageBytes.Length / 1024}KB, {tex.width}x{tex.height})");
#endif
                    return tex;
                }

                UnityEngine.Object.Destroy(tex);
                return null;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Mp3Cover] 추출 실패: {Path.GetFileName(mp3FilePath)} - {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Stream에서 직접 임베드된 커버 아트를 추출 (Android MediaStore InputStream 용).
        /// 실패 시 null 반환.
        /// </summary>
        public static Texture2D ExtractCoverFromStream(Stream stream, string labelForLog = "stream")
        {
            if (stream == null) return null;
            try
            {
                byte[] apicData = ReadApicFrameFromStream(stream);
                if (apicData == null || apicData.Length < 4) return null;

                byte[] imageBytes = ParseApicImageBytes(apicData);
                if (imageBytes == null || imageBytes.Length < 4) return null;

                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (tex.LoadImage(imageBytes))
                {
#if UNITY_EDITOR
                    Debug.Log($"[Mp3Cover] 앨범 아트 추출 성공(stream): {labelForLog} ({imageBytes.Length / 1024}KB, {tex.width}x{tex.height})");
#endif
                    return tex;
                }

                UnityEngine.Object.Destroy(tex);
                return null;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Mp3Cover] 스트림 추출 실패: {labelForLog} - {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Stream에서 APIC 프레임의 raw 바이트를 읽음 (Android InputStream 용).
        /// </summary>
        private static byte[] ReadApicFrameFromStream(Stream stream)
        {
            using (var br = new BinaryReader(stream, System.Text.Encoding.ASCII, leaveOpen: true))
            {
                return ReadApicFrameFromReader(br);
            }
        }

        /// <summary>
        /// MP3 파일에서 APIC 프레임의 raw 바이트를 읽음.
        /// ID3v2 헤더를 파싱한 후 APIC 프레임을 탐색.
        /// </summary>
        private static byte[] ReadApicFrame(string path)
        {
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var br = new BinaryReader(fs))
            {
                return ReadApicFrameFromReader(br);
            }
        }

        /// <summary>
        /// BinaryReader로부터 APIC 프레임을 탐색 (파일/스트림 공용).
        /// </summary>
        private static byte[] ReadApicFrameFromReader(BinaryReader br)
        {
            // ID3v2 헤더 확인 (10 bytes)
            byte[] id3Header = br.ReadBytes(10);
            if (id3Header.Length < 10) return null;
            if (id3Header[0] != 'I' || id3Header[1] != 'D' || id3Header[2] != '3')
                return null;

            byte majorVersion = id3Header[3]; // 2=ID3v2.2, 3=ID3v2.3, 4=ID3v2.4
            byte flags = id3Header[5];
            bool hasExtHeader = (flags & 0x40) != 0;

            // ID3v2 태그 전체 크기 (syncsafe integer, 4 bytes)
            int tagSize = DecodeSyncsafe(id3Header, 6);
            long tagEnd = 10 + tagSize;

            // Extended header 스킵
            if (hasExtHeader)
            {
                if (majorVersion == 3)
                {
                    int extSize = ReadInt32BE(br);
                    br.BaseStream.Seek(extSize - 4, SeekOrigin.Current);
                }
                else if (majorVersion == 4)
                {
                    int extSize = DecodeSyncsafe(br.ReadBytes(4), 0);
                    br.BaseStream.Seek(extSize - 4, SeekOrigin.Current);
                }
            }

            // 프레임 반복 탐색
            while (br.BaseStream.Position < tagEnd - 10)
            {
                long frameStart = br.BaseStream.Position;

                string frameId;
                int frameSize;

                if (majorVersion == 2)
                {
                    // ID3v2.2: 3바이트 ID + 3바이트 크기
                    byte[] idBytes = br.ReadBytes(3);
                    if (idBytes[0] == 0) break;
                    frameId = System.Text.Encoding.ASCII.GetString(idBytes);
                    byte[] sizeBytes = br.ReadBytes(3);
                    frameSize = (sizeBytes[0] << 16) | (sizeBytes[1] << 8) | sizeBytes[2];
                }
                else
                {
                    // ID3v2.3 / v2.4: 4바이트 ID + 4바이트 크기 + 2바이트 플래그
                    byte[] idBytes = br.ReadBytes(4);
                    if (idBytes[0] == 0) break;
                    frameId = System.Text.Encoding.ASCII.GetString(idBytes);

                    byte[] sizeBytes = br.ReadBytes(4);
                    frameSize = majorVersion == 4
                        ? DecodeSyncsafe(sizeBytes, 0)
                        : ReadInt32BE(sizeBytes);

                    br.ReadBytes(2); // flags
                }

                if (frameSize <= 0 || frameStart + frameSize > tagEnd + 100)
                    break;

                // APIC (v2.3+) 또는 PIC (v2.2) 프레임 발견
                if (frameId == "APIC" || frameId == "PIC ")
                {
                    return br.ReadBytes(frameSize);
                }

                // 다음 프레임으로 이동
                br.BaseStream.Seek(frameStart + (majorVersion == 2 ? 6 : 10) + frameSize,
                    SeekOrigin.Begin);
            }

            return null;
        }

        /// <summary>
        /// APIC 프레임 데이터에서 실제 이미지 바이트만 추출.
        /// 구조: [encoding(1)] [mime\0] [pictype(1)] [desc\0] [imagedata]
        /// </summary>
        private static byte[] ParseApicImageBytes(byte[] apicData)
        {
            int pos = 0;

            if (pos >= apicData.Length) return null;
            byte encoding = apicData[pos++]; // 0=Latin1, 1=UTF-16, 2=UTF-16BE, 3=UTF-8

            // MIME 타입 읽기 (null 종료)
            int mimeEnd = Array.IndexOf(apicData, (byte)0, pos);
            if (mimeEnd < 0) return null;
            // string mimeType = System.Text.Encoding.ASCII.GetString(apicData, pos, mimeEnd - pos);
            pos = mimeEnd + 1;

            // 그림 타입 (1바이트)
            if (pos >= apicData.Length) return null;
            // byte picType = apicData[pos];
            pos++;

            // 설명 읽기 (null 종료, encoding에 따라 1바이트 또는 2바이트 null)
            if (encoding == 1 || encoding == 2)
            {
                // UTF-16: 2바이트 null 터미네이터
                while (pos + 1 < apicData.Length)
                {
                    if (apicData[pos] == 0 && apicData[pos + 1] == 0)
                    {
                        pos += 2;
                        break;
                    }
                    pos += 2;
                }
            }
            else
            {
                // Latin1 / UTF-8: 1바이트 null 터미네이터
                int descEnd = Array.IndexOf(apicData, (byte)0, pos);
                if (descEnd < 0) return null;
                pos = descEnd + 1;
            }

            if (pos >= apicData.Length) return null;

            int imageLen = apicData.Length - pos;
            if (imageLen < 4) return null;

            byte[] imageBytes = new byte[imageLen];
            Buffer.BlockCopy(apicData, pos, imageBytes, 0, imageLen);
            return imageBytes;
        }

        // ── 유틸리티 ──────────────────────────────────────────────

        /// <summary>ID3v2 syncsafe integer 디코딩 (4바이트)</summary>
        private static int DecodeSyncsafe(byte[] data, int offset)
        {
            return ((data[offset] & 0x7F) << 21)
                 | ((data[offset + 1] & 0x7F) << 14)
                 | ((data[offset + 2] & 0x7F) << 7)
                 |  (data[offset + 3] & 0x7F);
        }

        /// <summary>Big-endian int32 읽기 (BinaryReader에서)</summary>
        private static int ReadInt32BE(BinaryReader br)
        {
            byte[] b = br.ReadBytes(4);
            return ReadInt32BE(b);
        }

        /// <summary>Big-endian int32 (byte[] 배열에서)</summary>
        private static int ReadInt32BE(byte[] b)
        {
            return (b[0] << 24) | (b[1] << 16) | (b[2] << 8) | b[3];
        }
    }
}
