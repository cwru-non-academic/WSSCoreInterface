﻿using System.Collections;
using UnityEngine;
using System;

public class Stimulation : MonoBehaviour
{
    [SerializeField] private bool forcePort = false;
    [SerializeField] private bool testMode = true;
    [SerializeField] private string comPort = "COM7";
    [SerializeField] private StimConfigController config;

    private const float delay = 0.1f;// delay between mesages to the WSS to avoid congestion on the radio
    private int maxWSS = 1;

    public bool started = false;
    private bool ready = false;
    private bool editor = false;
    private bool running = false;
    private bool setup = false;
    private SerialToWSS WSS;
    private float[] prevMagnitude;


    #region "Channels vars"
    private int[] ChAmps;
    private int[] ChPWs;
    private int current_IPD = 50; //in us
    #endregion

    // Start is called before the first frame update
    public void Start()
    {
        
    }

    void OnEnable()
    {
        config.LoadJSON();
        maxWSS=config._config.maxWSS;
        initStimVaribles();
        editor = Application.isEditor;
        if ((editor || Application.platform == RuntimePlatform.WindowsPlayer) && !testMode) //runs USB mode only on editor mode or windows mode 
        {
            if (!forcePort) //overide useful when multiple ports
            {
                WSS = new SerialToWSS();
            }
            else
            {
                WSS = new SerialToWSS(comPort);
            }
            NormalSetup();
            running = true;
        }
        else if (testMode)
        {
            running = false;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(!testMode)
        {
            WSS.checkForErrors();
            started = WSS.Started();
            if (WSS.isQueueEmpty() && setup)
            {
                ready = true;
            }
            if (WSS.msgs.Count>0)
            {
                for (int i = 0; i < WSS.msgs.Count; i++)
                {
                    if (WSS.msgs[i].StartsWith("Error:")){
                        Debug.LogError(WSS.msgs[i]);
                    }else
                    {
                        Debug.Log(WSS.msgs[i]);
                    }
                    WSS.msgs.RemoveAt(i);
                }
            }
        }
    }

    void OnDestroy()
    {
        
    }

    void OnDisable()
    {
        if (!testMode)
        {
            running = false;
            WSS.zero_out_stim();
            WSS.releaseCOM_port();
            ready = false;
        }
    }

    #region "Stimulation methods"
    //current functions are design to be called by a discrete sensor  
    // like the bubble. For an analog sensor I suggets you only use the 
    //function below and disregard the rest
    public void StimulateAnalog(string finger, bool rawValues, int PW, int amp = 3)
    {
        //finger name of the finger (shown in if statements below) or channel name
        //right bool true for right hand false for left (not implemented yet)
        //PW is pulse width of the stimualtion in the range 4 to 255 us or 0us for no stim
        //Amp is amplitude of the stimualtion in the rnage of 0 to 83mA (non linear or exact).
        int channel = 0;
        if (finger == "Index")
        {
            channel = 2;
        }
        else if (finger == "Middle")
        {
            channel = 3;
        }
        else if (finger == "Ring")
        {
            channel = 4;
        }
        else if (finger == "Pinky")
        {
            channel = 5;
        }
        else if (finger == "Thumb")
        {
            channel = 1;
        }
        else if (finger == "Palm")
        {
            channel = 6;
        }
        else if (finger.Substring(0,2) == "ch")
        {
            try
            {
                channel = Int32.Parse(finger.Substring(2));
            } catch (FormatException e)
            { 
                Debug.LogError("Stimualtion channel not a number: "+ e.Message);
                channel = 0;
            }
        }

        if (channel > 0 && channel<maxWSS*3)
        {
            ChAmps[channel-1] = amp;
            ChPWs[channel-1] = PW;
        }
    }

    private void initStimVaribles()
    {
        ChAmps = new int[maxWSS * 3];
        ChPWs = new int[maxWSS * 3];
        prevMagnitude = new float[maxWSS * 3];
        for (int i = 0; i < ChAmps.Length; i++)//initilize parameters at 0
        {
            ChAmps[i] = 0;
            ChPWs[i] = 0;
            prevMagnitude[i] = 0;
        }
    }

    IEnumerator UpdateCoroutine()
    {
        while(running)
        {
            while (started && ready)
            {
                WSS.stream_change(1, new int[] { AmpTo255Convention(ChAmps[0]), AmpTo255Convention(ChAmps[1]), AmpTo255Convention(ChAmps[2]) },
                    new int[] { ChPWs[0], ChPWs[1], ChPWs[2] }, null);
                yield return new WaitForSeconds(0.02f);
                if (maxWSS > 1)
                {
                    WSS.stream_change(2, new int[] { AmpTo255Convention(ChAmps[3]), AmpTo255Convention(ChAmps[4]), AmpTo255Convention(ChAmps[5]) },
                    new int[] { ChPWs[3], ChPWs[4], ChPWs[5] }, null);
                    yield return new WaitForSeconds(0.02f);
                }
            }
            yield return new WaitForSeconds(0.02f);
        }
    }

    public void StartStimulation()
    {
        if (testMode)
        {
            return;
        }
        ready = false;
        WSS.startStim();
        Debug.Log("sent start stim msg");
        StartCoroutine(UpdateCoroutine());
    }


    public void StopStimulation()
    {
        if (testMode)
        {
            return;
        }
        ready = false;
        WSS.stopStim();
        Debug.Log("sent stop stim msg");
        running = false;
    }

    public void StimWithMode(string finger, float magnitude)
    {
        int channel = 0;
        if (finger == "Index")
        {
            channel = 2;
        }
        else if (finger == "Middle")
        {
            channel = 3;
        }
        else if (finger == "Ring")
        {
            channel = 4;
        }
        else if (finger == "Pinky")
        {
            channel = 5;
        }
        else if (finger == "Thumb")
        {
            channel = 1;
        }
        else if (finger == "Palm")
        {
            channel = 6;
        }
        else if (finger.Substring(0, 2) == "ch")
        {
            try
            {
                channel = Int32.Parse(finger.Substring(2));
            }
            catch (FormatException e)
            {
                Debug.LogError(e.Message);
                channel = 0;
            }
        }

        if (channel > 0 && channel <= maxWSS * 3)
        {
            ChAmps[channel - 1] = (int)config.getStimParam("Ch"+channel.ToString()+"Amp"); ;
            ChPWs[channel - 1] = calculateStim(channel, magnitude, config.getStimParam("Ch"+channel.ToString() + "Max"), config.getStimParam("Ch" + channel.ToString() +"Min")); ;
        }
    }

    public void UpdateChannelParams(string finger, int max, int min, int amp)
    {
        int channel = 0;
        if (finger == "Index")
        {
            channel = 2;
        }
        else if (finger == "Middle")
        {
            channel = 3;
        }
        else if (finger == "Ring")
        {
            channel = 4;
        }
        else if (finger == "Pinky")
        {
            channel = 5;
        }
        else if (finger == "Thumb")
        {
            channel = 1;
        }
        else if (finger == "Palm")
        {
            channel = 6;
        }
        else if (finger.Substring(0, 2) == "ch")
        {
            try
            {
                channel = Int32.Parse(finger.Substring(2));
            }
            catch (FormatException e)
            {
                Debug.LogError(e.Message);
                channel = 0;
            }
        }

        if (channel > 0 && channel <= maxWSS * 3)
        {
            config.modifyStimParam("Ch" + channel.ToString() + "Amp", amp);
            config.modifyStimParam("Ch" + channel.ToString() + "Max", max);
            config.modifyStimParam("Ch" + channel.ToString() + "Min", min);
        }
    }

    private int calculateStim(int channel, float magnitude, float max, float min)
    {
        float output = 0;
        //apply stimulation controller mode equation to magnitude
        if (config._config.sensationController == "P")
        {
            output = magnitude * config.getConstant("PModeProportional") + config.getConstant("PModeOffsset");
        } else if (config._config.sensationController == "PD")
        {
            float d = (magnitude - prevMagnitude[channel])/Time.deltaTime;
            output= (d*config.getConstant("PDModeDerivative"))+(magnitude*config.getConstant("PDModeProportional"))+ config.getConstant("PDModeOffsset");
            prevMagnitude[channel] = magnitude;
        }

        //handle case that could go above maxiumum or negative
        if (output > 1)
        {
            output = 1;
        } else if (output < 0)
        {
            output = 0;
        }
        if (output > 0)
        {
            output = (output * (max - min)) + min;
        } else
        {
            output = 0;
        }
        return (int) output;
    }

    public void NormalSetup()
    {
        for (int i = 1; i < maxWSS+1; i++)
        {
            //gnd is first electrode 
            // WSS 1 thumb index middle
            WSS.clear(i, 0); //clear everything to make sure setup is correct
            WSS.create_schedule(i, 1, 13, 170); //create a schedule to be sync with the 170 int signal  //13ms is period so freq of about 77Hz
            WSS.creat_contact_config(i, 1, new int[] { 0, 0, 2, 1 }, new int[] { 0, 0, 1, 2 }); //create a 2 cathodes and 1 anode for stim ch1
            WSS.create_event(i, 1, 0, 1, 0, 0, new int[] { 11, 11, 0, 0 }, new int[] { 11, 11, 0, 0 }, new int[] { 0, 0, 50 }); //create a 3mA square wave with 0 PW and 50us IPD for ch1
            WSS.edit_event_ratio(i, 1, 8); //make the wave symetric
            WSS.add_event_to_schedule(i, 1, 1);
            WSS.create_schedule(i, 2, 13, 170); //create a schedule to be sync with the 170 int signal  //13ms is period so freq of about 77Hz
            WSS.creat_contact_config(i, 2, new int[] { 0, 2, 0, 1 }, new int[] { 0, 1, 0, 2 }); //create a 1 cathodes and 1 anode for stim ch2
            WSS.create_event(i, 2, 2, 2, 0, 0, new int[] { 11, 0, 11, 0 }, new int[] { 11, 0, 11, 0 }, new int[] { 0, 0, 50 }); //create a 3mA square wave with 0 PW and 50us IPD for ch2
            WSS.edit_event_ratio(i, 2, 8); //make the wave symetric
            WSS.add_event_to_schedule(i, 2, 2);
            WSS.create_schedule(i, 3, 13, 170); //create a schedule to be sync with the 170 int signal  //13ms is period so freq of about 77Hz
            WSS.creat_contact_config(i, 3, new int[] { 2, 0, 0, 1 }, new int[] { 1, 0, 0, 2 }); //create a 1 cathodes and 1 anode for stim ch3
            WSS.create_event(i, 3, 4, 3, 0, 0, new int[] { 11, 0, 0, 11 }, new int[] { 11, 0, 0, 11 }, new int[] { 0, 0, 50 }); //create a 3mA square wave with 0 PW and 50us IPD for ch3
            WSS.edit_event_ratio(i, 3, 8); //make the wave symetric
            WSS.add_event_to_schedule(i, 3, 3);
            WSS.sync_group(i, 170); //sync schedules with 170 sync signal
        }
        setup = true;
    }

    public void Save(int targetWSS)
    {
        if (testMode)
        {
            return;
        }
        ready = false;
        WSS.populateFRAMSettings(targetWSS);
    }

    public void Save()
    {
        if (testMode)
        {
            return;
        }
        ready = false;
        for (int i = 1; i < maxWSS + 1; i++)
        {
            WSS.populateFRAMSettings(i);
        }
    }

    public void load(int targetWSS)
    {
        if (testMode)
        {
            return;
        }
        ready = false;
        WSS.populateBoardSettings(targetWSS);
    }

    public void load()
    {
        if (testMode)
        {
            return;
        }
        ready = false;
        for (int i = 1; i < maxWSS + 1; i++)
        {
            WSS.populateBoardSettings(i);
        }
    }

    //deprecated as stream command now handles both PW and AMP
    /*public void UpdateAllPA(int PA) //in mA (0 to 83mA)
    {
        
    }*/

    //transform mA to 0 to 255 byte convetion
    private int AmpTo255Convention(int amp) {
        if(amp <4)
        {
            return (int) Mathf.Pow(amp /0.0522f, 1/1.5466f)+1;
        } else
        {
            return (int) ((amp + 1.7045f)/ 0.3396f)+1;
        }
    }

    public void request_Configs(int targetWSS, int command, int id)
    {
        if (testMode)
        {
            return;
        }
        ready = false;
        WSS.request_configs(targetWSS, command, id);
    }

    public void UpdateIPD(int targetWSS, int IPD) // in us (0 to 1000us)
    {
        if (testMode)
        {
            return;
        }
        ready = false;
        if (IPD > 1000)
        {
            IPD = 1000;
        }
        current_IPD = IPD;
        WSS.edit_event_PW(targetWSS, 1, new int[] { 0, 0, current_IPD }); //edit ch 1
        WSS.edit_event_PW(targetWSS, 2, new int[] { 0, 0, current_IPD }); //edit ch 2
        WSS.edit_event_PW(targetWSS, 3, new int[] { 0, 0, current_IPD }); //edit ch 3
    }

    public void UpdateIPD(int IPD) // in us (0 to 1000us)
    {
        if (testMode)
        {
            return;
        }
        ready = false;
        if (IPD > 1000)
        {
            IPD = 1000;
        }
        current_IPD = IPD;
        for (int i = 1; i < maxWSS + 1; i++)
        {
            WSS.edit_event_PW(i, 1, new int[] { 0, 0, current_IPD }); //edit ch 1
            WSS.edit_event_PW(i, 2, new int[] { 0, 0, current_IPD }); //edit ch 2
            WSS.edit_event_PW(i, 3, new int[] { 0, 0, current_IPD }); //edit ch 3
        }
    }

    public void UpdateFrequency(int targetWSS, int FR) //in Hz (1-1000Hz) might be further limited by PW duration
    {
        if (testMode)
        {
            return;
        }
        ready = false;
        float temp = 1000.0f / FR;
        int Freq = (int)temp; //in ms now
                                //sets PW to 0 too
        WSS.stream_change(targetWSS, null, new int[] { 0, 0, 0 }, new int[] { Freq, Freq, Freq });
    } //max 1000ms for pw IPD

    public void UpdateFrequency(int FR) //in Hz (1-1000Hz) might be further limited by PW duration
    {
        if (testMode)
        {
            return;
        }
        ready = false;
        float temp = 1000.0f / FR;
        int Freq = (int)temp; //in ms now
                                //sets PW to 0 too
        for (int i = 1; i < maxWSS + 1; i++)
        {
            WSS.stream_change(i, null, new int[] { 0, 0, 0 }, new int[] { Freq, Freq, Freq });
        }
    } //max 1000ms for pw IPD

    public void updateWaveform(int[] waveform, int eventID) 
    {
        if (testMode)
        {
            return;
        }
        ready = false;
        WaveformBuilder stimShape = new WaveformBuilder(waveform);
        WaveformSetup(stimShape, eventID);
    }

    public void updateWaveform(int targetWSS, int[] waveform, int eventID)
    {
        if (testMode)
        {
            return;
        }
        ready = false;
        WaveformBuilder stimShape = new WaveformBuilder(waveform);
        WaveformSetup(targetWSS, stimShape, eventID);
    }

    public void updateWaveform(int cathodicWaveform, int anodicWaveform, int eventID) //overload to just select from waveforms in memory 
    //slots 0 to 10 are predefined waveforms and slots 11 to 13 are custom defined waveforms
    {
        if (testMode)
        {
            return;
        }
        ready = false;
        for (int i = 1; i < maxWSS + 1; i++)
        {
            WSS.edit_event_shape(i, eventID, cathodicWaveform, anodicWaveform);
        }
    }

    public void updateWaveform(int targetWSS, int cathodicWaveform, int anodicWaveform, int eventID) //overload to just select from waveforms in memory 
    //slots 0 to 10 are predefined waveforms and slots 11 to 13 are custom defined waveforms
    {
        if (testMode)
        {
            return;
        }
        ready = false;
        WSS.edit_event_shape(targetWSS, eventID, cathodicWaveform, anodicWaveform);
    }

    //overload for loading from json functionality
    public void updateWaveform(WaveformBuilder waveform, int eventID) 
    {
        if (testMode)
        {
            return;
        }
        ready = false;
        WaveformBuilder stimShape = waveform;
        WaveformSetup(stimShape, eventID);
    }

    public void updateWaveform(int targetWSS, WaveformBuilder waveform, int eventID)
    {
        if (testMode)
        {
            return;
        }
        ready = false;
        WaveformBuilder stimShape = waveform;
        WaveformSetup(targetWSS, stimShape, eventID);
    }

    public void loadWaveform(string fileName, int eventID)
    {
        try
        {
            Waveform shape = JsonUtility.FromJson<Waveform>(System.IO.File.ReadAllText(Application.streamingAssetsPath + "/" + fileName + "WF.json"));
            updateWaveform(new WaveformBuilder(shape), eventID);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("JSON loading error: " + ex.Message);
        }
    }


    public void WaveformSetup(WaveformBuilder wave, int eventID)//custom waveform slots 0 to 2 are attached to shape slots 11 to 13
    {
        if (testMode)
        {
            return;
        }
        for (int i = 1; i < maxWSS + 1; i++)
        {
            WSS.set_costume_waveform(i, 0, wave.getCatShapeArray()[0..^24], 0);
            WSS.set_costume_waveform(i, 0, wave.getCatShapeArray()[8..^16], 1);
            WSS.set_costume_waveform(i, 0, wave.getCatShapeArray()[16..^8], 2);
            WSS.set_costume_waveform(i, 0, wave.getCatShapeArray()[24..^0], 3);
            WSS.set_costume_waveform(i, 1, wave.getAnodicShapeArray()[0..^24], 0);
            WSS.set_costume_waveform(i, 1, wave.getAnodicShapeArray()[8..^16], 1);
            WSS.set_costume_waveform(i, 1, wave.getAnodicShapeArray()[16..^8], 2);
            WSS.set_costume_waveform(i, 1, wave.getAnodicShapeArray()[24..^0], 3);
            WSS.edit_event_shape(i, eventID, 11, 12);
        }
    }

    public void WaveformSetup(int targetWSS, WaveformBuilder wave, int eventID)//custom waveform slots 0 to 2 are attached to shape slots 11 to 13
    {
        if (testMode)
        {
            return;
        }
        WSS.set_costume_waveform(targetWSS, 0, wave.getCatShapeArray()[0..^24], 0);
        WSS.set_costume_waveform(targetWSS, 0, wave.getCatShapeArray()[8..^16], 1);
        WSS.set_costume_waveform(targetWSS, 0, wave.getCatShapeArray()[16..^8], 2);
        WSS.set_costume_waveform(targetWSS, 0, wave.getCatShapeArray()[24..^0], 3);
        WSS.set_costume_waveform(targetWSS, 1, wave.getAnodicShapeArray()[0..^24], 0);
        WSS.set_costume_waveform(targetWSS, 1, wave.getAnodicShapeArray()[8..^16], 1);
        WSS.set_costume_waveform(targetWSS, 1, wave.getAnodicShapeArray()[16..^8], 2);
        WSS.set_costume_waveform(targetWSS, 1, wave.getAnodicShapeArray()[24..^0], 3);
        WSS.edit_event_shape(targetWSS, eventID, 11, 12);
    }
    #endregion

    #region getSets
    public bool Ready()
    {
        return ready;
    }
    #endregion
}
