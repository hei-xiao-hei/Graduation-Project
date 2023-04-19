using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;
using VRTK;
using Valve.VR;

public class VioceInteraction : MonoBehaviour
{
    private const string app_id = "appid = 90211c66";
    private const string session_begin_params = "sub = iat, domain = iat, language = zh_cn, accent = mandarin, sample_rate = 16000, result_type = plain, result_encoding = utf-8";
    private IntPtr session_id;

    public AudioSource audios; //存储录制的音频
    private int frequency = 16000;//采样率

    public GameObject Controllers;
    private VRTK_ControllerEvents Events;//获取手柄控制器事件


    private bool VoiceTrue;//语音识别是否正确

    private void Awake()
    {
        Events = Controllers.transform.GetComponent<VRTK_ControllerEvents>();
    }
    // Start is called before the first frame update
    void Start()
    {
        Login(app_id);//登录
        //注册侧握按键的事件
        Events.GripPressed += Events_GripPressed;
        VoiceTrue = false;
    }
    //按下握持键
    private void Events_GripPressed(object sender, ControllerInteractionEventArgs e)
    {
        Debug.Log("握持键按下了");
        VoiceTrue = false;//每次识别前先重置一下
        //如果选对了，并且按下了握持键，开始语音识别
        if (Training.training.isRight)
        {
            Debug.Log("答对啦，开始训练");
            SoundRecording();
            
        }
        
    }

    private void OnDestroy()
    {
        logOut();//退出登录
    }

    //登录
    private bool Login(string my_appid)
    {
        //用户名，密码，登陆信息，前两个均为空
        int res = MSCDLL.MSPLogin(null, null, my_appid);

        if (res != (int)Errors.MSP_SUCCESS) //说明登陆失败
        {
            Debug.Log("登陆失败！");
            Debug.Log(my_appid);
            Debug.Log("错误编号: " + res);
            return false;
        }

        Debug.Log("登陆成功！");
        return true;
    }


    //建立会话
    private void sessionBegin(string session_begin_params)
    {
        int errcode = (int)Errors.MSP_SUCCESS;

        session_id = MSCDLL.QISRSessionBegin(null, session_begin_params, ref errcode);
        if (errcode != (int)Errors.MSP_SUCCESS)
        {
            Debug.Log("建立会话失败！");
            Debug.Log("错误编号: " + errcode);
        }
    }

    //结束对话
    private bool sessionEnd()
    {
        string hints = "hiahiahia";
        int res;

        res = MSCDLL.QISRSessionEnd(session_id, hints);
        if (res != (int)Errors.MSP_SUCCESS)
        {
            Debug.Log("会话结束失败！");
            Debug.Log("错误编号:" + res);
            return false;
        }
        return true;
    }


    //退出登录
    private bool logOut()
    {
        int res;

        res = MSCDLL.MSPLogout();
        if (res != (int)Errors.MSP_SUCCESS)
        {//说明登陆失败
            Debug.Log("退出登录失败！");
            Debug.Log("错误编号:" + res);
            return false;
        }
        Debug.Log("退出登录成功！");
        return true;
    }


    //将二进制字节转换成音频
    private bool audio_iat(byte[] AudioData)
    {
        var aud_stat = AudioStatus.MSP_AUDIO_SAMPLE_CONTINUE;//音频状态
        var ep_stat = EpStatus.MSP_EP_LOOKING_FOR_SPEECH;//端点状态
        var rec_stat = RecogStatus.MSP_REC_STATUS_SUCCESS;//识别状态
        byte[] audio_content = AudioData;

        //写入音频
        int res = MSCDLL.QISRAudioWrite(session_id, audio_content, (uint)audio_content.Length, aud_stat, ref ep_stat, ref rec_stat);
        if (res != (int)Errors.MSP_SUCCESS)
        {
            Debug.Log("写入音频失败！");
            Debug.Log("错误编号: " + res);
            return false;
        }
        //告知识别结束
        res = MSCDLL.QISRAudioWrite(session_id, null, 0, AudioStatus.MSP_AUDIO_SAMPLE_LAST, ref ep_stat, ref rec_stat);
        if (res != (int)Errors.MSP_SUCCESS)
        {
            Debug.Log("写入音频结束失败！" + res);
            Debug.Log("错误编号: " + res);
            return false;
        }

        int errcode = (int)Errors.MSP_SUCCESS;
        StringBuilder result = new StringBuilder();//存储最终识别的结果
        int totalLength = 0;//用来记录总的识别后的结果的长度，判断是否超过缓存最大值

        while (RecogStatus.MSP_REC_STATUS_COMPLETE != rec_stat)
        {   //如果没有完成就一直继续获取结果
            IntPtr now_result = MSCDLL.QISRGetResult(session_id, ref rec_stat, 0, ref errcode);
            if (errcode != (int)Errors.MSP_SUCCESS)
            {
                Debug.Log("获取结果失败：");
                Debug.Log("错误编号: " + errcode);
                return false;
            }
            if (now_result != null)
            {
                int length = now_result.ToString().Length;
                totalLength += length;
                if (totalLength > 4096)
                {
                    Debug.Log("缓存空间不够" + totalLength);
                    return false;
                }
                result.Append(Marshal.PtrToStringAnsi(now_result));
            }
            Thread.Sleep(150);//防止频繁占用cpu
        }
        Debug.Log("语音听写结束，结果为： \n ");
        Debug.Log(result.ToString());
        string results = result.ToString();//将语音识别的内容转换成字符串
        //判断语音识别出来的文字和正确答案是否一致
        switch (Training.training.topicIndex)
        {
            case 0:
                if(results=="这个是什么？")
                {
                    VoiceTrue = true;
                }
                break;
            case 1:
                if (results == "真热闹。")
                {
                    VoiceTrue = true;
                }
                break;
            case 2:
                if (results == "这是西红柿。")
                {
                    VoiceTrue = true;
                }
                break;
        }

        
        return true;

    }

    //将音频转换成二进制的字节
    private byte[] convertClipToBytes(AudioClip clip)
    {
        //clip.length;
        float[] samples = new float[clip.samples];

        clip.GetData(samples, 0);

        short[] intData = new short[samples.Length];
        //converting in 2 float[] steps to Int16[], //then Int16[] to Byte[]

        byte[] bytesData = new byte[samples.Length * 2];
        //bytesData array is twice the size of
        //dataSource array because a float converted in Int16 is 2 bytes.

        int rescaleFactor = 32767; //to convert float to Int16

        for (int i = 0; i < samples.Length; i++)
        {
            intData[i] = (short)(samples[i] * rescaleFactor);
            byte[] byteArr = new byte[2];
            byteArr = BitConverter.GetBytes(intData[i]);
            byteArr.CopyTo(bytesData, i * 2);
        }
        return bytesData;
    }

    //识别玩家录音并输出播放录制玩家语音
    public void SoundRecording()
    {
        audios.clip = Microphone.Start(null, false, 5, frequency);//开始录制音频,5秒
        Invoke("Delay", 5.0f);
        //StartCoroutine(Delay());
        //Thread.Sleep(5000);//5秒后停止录音
        /*byte[] audioData = convertClipToBytes(audios.clip);//将录音转换成字节
        sessionBegin(session_begin_params);//建立会话
        audio_iat(audioData);//将字节转换成音频
        audios.Play();//播放音频
        sessionEnd();//结束对话*/

    }
    private void Delay()
    {
        byte[] audioData = convertClipToBytes(audios.clip);//将录音转换成字节
        sessionBegin(session_begin_params);//建立会话
        audio_iat(audioData);//将字节转换成音频
        audios.Play();//播放音频
        sessionEnd();//结束对话
        //如果语音识别正确则进入下一题
        if (VoiceTrue)
        {
            Debug.Log("进入了这里");
            Training.training.NextQ();//下一题
        }
        else
        {
            Training.training.Again();//再听一次老师讲啥
        }
    }
    //public AudioSource audio;//音频源

    #region 登录
    //验证登录情况
    /*private bool login(string my_appid)
    {
        //用户名，密码，登录信息，前两个均为空
        int res = MSCDLL.MSPLogin(null, null, my_appid);//获得登录信息

        if (res != (int)Errors.MSP_SUCCESS)
        {
            Debug.Log("登录失败！");
            Debug.Log(my_appid);
            Debug.Log("错误编号：" + res);
            return false;
        }
        return true;
    }*/
    #endregion

    #region 建立会话
    //建立会话
    /*      * QISRSessionBegin（）；
        * 功能：开始一次语音识别
        * 参数1：定义关键词识别||语法识别||连续语音识别（null）
        * 参数2：设置识别的参数：语言、领域、语言区域
        * 参数3：带回语音识别的结果，成功||错误代码
        * 返回值intPtr类型,后面会用到这个返回值  */

    /*private const string session_begin_params = "sub = iat, domain = iat, language = zh_cn, accent = mandarin, sample_rate = 16000, result_type = plain, result_encoding = utf-8";
        private IntPtr session_id;

        private void sessionBegin(string session_begin_params)
        {
            int errcode = (int)Errors.MSP_SUCCESS;

            session_id = MSCDLL.QISRSessionBegin(null, session_begin_params, ref errcode);//建立会话
            if (errcode != (int)Errors.MSP_SUCCESS)
            {
                Debug.Log("建立会话失败！");
                Debug.Log("错误编号: " + errcode);
            }
        }*/
    #endregion

    #region 语音识别
    //语音识别
    /*
         QISRAudioWrite（）；
         功能：写入本次识别的音频
         参数1：之前已经得到的sessionID
         参数2：音频数据缓冲区起始地址
         参数3：音频数据长度,单位字节。
          参数4：用来告知MSC音频发送是否完成     MSP_AUDIO_SAMPLE_FIRST = 1第一块音频
                                                  MSP_AUDIO_SAMPLE_CONTINUE = 2还有后继音频
                                                   MSP_AUDIO_SAMPLE_LAST = 4最后一块音频
         参数5：端点检测（End-point detected）器所处的状态
                                                MSP_EP_LOOKING_FOR_SPEECH = 0还没有检测到音频的前端点。
                                                 MSP_EP_IN_SPEECH = 1已经检测到了音频前端点，正在进行正常的音频处理。
                                                 MSP_EP_AFTER_SPEECH = 3检测到音频的后端点，后继的音频会被MSC忽略。
                                                  MSP_EP_TIMEOUT = 4超时。
                                                 MSP_EP_ERROR = 5出现错误。
                                                 MSP_EP_MAX_SPEECH = 6音频过大。
         参数6：识别器返回的状态，提醒用户及时开始\停止获取识别结果
                                       MSP_REC_STATUS_SUCCESS = 0识别成功，此时用户可以调用QISRGetResult来获取（部分）结果。
                                        MSP_REC_STATUS_NO_MATCH = 1识别结束，没有识别结果。
                                      MSP_REC_STATUS_INCOMPLETE = 2正在识别中。
                                      MSP_REC_STATUS_COMPLETE = 5识别结束。
         返回值：函数调用成功则其值为MSP_SUCCESS，否则返回错误代码。
           本接口需不断调用，直到音频全部写入为止。上传音频时，需更新audioStatus的值。具体来说:
           当写入首块音频时,将audioStatus置为MSP_AUDIO_SAMPLE_FIRST
           当写入最后一块音频时,将audioStatus置为MSP_AUDIO_SAMPLE_LAST
           其余情况下,将audioStatus置为MSP_AUDIO_SAMPLE_CONTINUE
           同时，需定时检查两个变量：epStatus和rsltStatus。具体来说:
           当epStatus显示已检测到后端点时，MSC已不再接收音频，应及时停止音频写入
           当rsltStatus显示有识别结果返回时，即可从MSC缓存中获取结果*/
    /*private bool audio_iat(byte[] AudioData)
    {
        var aud_stat = AudioStatus.MSP_AUDIO_SAMPLE_CONTINUE;//音频状态
        var ep_stat = EpStatus.MSP_EP_LOOKING_FOR_SPEECH;//端点状态
        var rec_stat = RecogStatus.MSP_REC_STATUS_SUCCESS;//识别状态
        byte[] audio_content = AudioData;

        int res = MSCDLL.QISRAudioWrite(session_id, audio_content, (uint)audio_content.Length, aud_stat, ref ep_stat, ref rec_stat);
        if (res != (int)Errors.MSP_SUCCESS)
        {
            Debug.Log("写入音频失败！");
            Debug.Log("错误编号: " + res);
            return false;
        }
        //告知识别结束
        res = MSCDLL.QISRAudioWrite(session_id, null, 0, AudioStatus.MSP_AUDIO_SAMPLE_LAST, ref ep_stat, ref rec_stat);
        if (res != (int)Errors.MSP_SUCCESS)
        {
            Debug.Log("写入音频结束失败！");

            Debug.Log("错误编号: " + res);
             return false;
        }
        //添加获取结果代码
        return true;
    }*/
    #endregion

    #region 获取结果
    //获取结果
    /*      
             QISRGetResult（）；
              功能：获取识别结果
              参数1：session，之前已获得
              参数2：识别结果的状态
              参数3：waitTime[in]此参数做保留用
              参数4：错误编码||成功
              返回值：函数执行成功且有识别结果时，返回结果字符串指针；其他情况(失败或无结果)返回NULL。
        */
    /*private bool audio_getresult()
    {
        
        
        int errcode = (int)Errors.MSP_SUCCESS;
        StringBuilder result = new StringBuilder();//存储最终识别的结果
        int totalLength = 0;//用来记录总的识别后的结果的长度，判断是否超过缓存最大值
        var rec_stat = RecogStatus.MSP_REC_STATUS_SUCCESS;//识别状态

        while (RecogStatus.MSP_REC_STATUS_COMPLETE != rec_stat)
        {//如果没有完成就一直继续获取结果
            IntPtr now_result = MSCDLL.QISRGetResult(session_id, ref rec_stat, 0, ref errcode);
            if (errcode != (int)Errors.MSP_SUCCESS)
            {
                Debug.Log("获取结果失败：");
                Debug.Log("错误编号: " + errcode);
                return false;
            }
            if (now_result != null)
            {
                int length = now_result.ToString().Length;
                totalLength += length;
                if (totalLength > 4096)
                {
                    Debug.Log("缓存空间不够" + totalLength);
                    return false;
                }
                result.Append(Marshal.PtrToStringAnsi(now_result));
            }
            Thread.Sleep(150);//防止频繁占用cpu
        }
        Debug.Log("语音听写结束，结果为： \n ");
        Debug.Log(result.ToString());
        return true;
    }*/
    #endregion

    #region 结束会话
    /*private bool sessionEnd()
    {
        string hints = "hiahiahia";
        int res;

        res = MSCDLL.QISRSessionEnd(session_id, hints);
        if (res != (int)Errors.MSP_SUCCESS)
        {
            Debug.Log("会话结束失败！");
            Debug.Log("错误编号:" + res);
            return false;
        }
        return true;
    }*/
    #endregion

    #region 退出登录
    //退出登录
    /*private bool logOut()
    {
        int res;
        res = MSCDLL.MSPLogout();
        if(res!=(int)Errors.MSP_SUCCESS)
        {
            //说明登录失败
            Debug.Log("退出登录失败");
            Debug.Log("错误编号：" + res);
            return false;
        }
        Debug.Log("退出登录成功");
        return true;
    }*/
    #endregion


}
