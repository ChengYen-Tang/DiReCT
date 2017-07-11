﻿/*
 * Copyright (c) 2016 Academia Sinica, Institude of Information Science
 *
 * License:
 *      GPL 3.0 : The content of this file is subject to the terms and 
 *      conditions defined in file 'COPYING.txt', which is part of this source
 *      code package.
 *
 * Project Name:
 * 
 *      DiReCT(Disaster Record Capture Tool)
 * 
 * File Description:
 * File Name:
 * 
 *      DMModule.cs
 * 
 * Abstract:
 *      
 *      Data Manager (DM) provides functions for other modules 
 *      to access the event data, user data and record data 
 *      in the local storage.
 *
 * Authors:
 * 
 *      Hunter Hsieh, hunter205@iis.sinica.edu.tw  
 *      Jeff Chen, jeff@iis.sinica.edu.tw
 * 
 */

using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;

using DiReCT.Model.Utilities;
using DiReCT;
using System.Collections;
using DiReCT.Model;
using DiReCT.Model.Observations;

namespace DiReCT
{
    class DMModule
    {
        static ModuleControlDataBlock moduleControlDataBlock;
        static ThreadParameters threadParameters;

        static ManualResetEvent ModuleAbortEvent, ModuleStartWorkEvent;
        static AutoResetEvent ModuleReadyEvent;

        static DiReCTThreadPool moduleThreadPool;
        static PriorityWorkQueue<WorkItem> ModuleWorkQueue;

        static DictionaryManager dictionary;


        //static IDictionary dictionary;

        const int MAX_NUMBER_OF_THREADS = 10;

        public static void DMInit(object objectParameters)
        {
            moduleControlDataBlock
                = (ModuleControlDataBlock)objectParameters;
            threadParameters = moduleControlDataBlock.ThreadParameters;
            //moduleWorkQueue = moduleControlDataBlock.ModuleWorkQueue;

            try
            {
                //Initialize Ready/Abort Event      
                ModuleReadyEvent = threadParameters.ModuleReadyEvent;
                ModuleAbortEvent = threadParameters.ModuleAbortEvent;
                ModuleWorkQueue = threadParameters.ModuleWorkQueue;
                moduleThreadPool = new DiReCTThreadPool(MAX_NUMBER_OF_THREADS);

                ModuleReadyEvent.Set();

                Debug.WriteLine("DMInit complete Phase 1 Initialization");

                //Wait for core StartWork Signal
                ModuleStartWorkEvent = threadParameters.ModuleStartWorkEvent;
                ModuleStartWorkEvent.WaitOne();

                Debug.WriteLine("DMInit complete Phase 2 Initialization");

                //
                // Main Thread of DM module (begin)
                //               
                dictionary = new DictionaryManager();

                Debug.WriteLine("DM module is working...");

                // Check ModuleAbortEvent periodically
                while (!ModuleAbortEvent
                        .WaitOne((int)TimeInterval.VeryVeryShortTime))
                {
                    //Does nothing
                    
                }

                Debug.WriteLine("DM module is aborting.");
                CleanupExit();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine("DM module thread failed.");
                threadParameters.ModuleInitFailedEvent.Set();
                Debug.WriteLine("DM ModuleInitFailedEvent Set");
                CleanupExit();
            }
        }

        private static void CleanupExit()
        {
            //
            // Cleanup code
            //
            Debug.WriteLine("DM module stopped successfully.");
            return;
        }

        internal static void DMWorkerFunctionProcessor(WorkItem workItem)
        {

            

            switch (workItem.AsyncCallName)
            {
                case AsyncCallName.SaveRecord:
                    
                    SendRecordToRTQC(workItem);
                    break;

            }
        }


        /// <summary>
        /// DM API to wrap up workItem and enqueue
        /// </summary>
        /// <param name="asyncCallName"></param>
        /// <param name="callBackFunction"></param>
        /// <param name="inputParameter"></param>
        /// <param name="state"></param>
        public static void DMWrapWorkItem(AsyncCallName asyncCallName,
                                          AsyncCallback callBackFunction,
                                          Object inputParameter,
                                          Object state)
        {

            WorkItem workItem = new WorkItem(
                FunctionGroupName.DataManagementFunction,
                asyncCallName,
                inputParameter,
                callBackFunction,
                state);

            moduleThreadPool.AddThreadWork(workItem);

        }


        /// <summary>
        /// Pass record to RTQC for validate
        /// </summary>
        /// <param name="workItem"></param>
        internal static void SendRecordToRTQC(WorkItem workItem)
        {
            //random Data
            int index = (int)workItem.InputParameters;
            ObservationRecord record;
            
            //Get the record from buffer 
            if (DiReCTCore.GetRecordFromBuffer(index, out record))
            {
                //Call RTQC API
                RTQCModule.RTQCWrapWorkItem(AsyncCallName.Validate,
                    new AsyncCallback(SaveRecordtoDictionary),
                    record,
                    null);
            }
            else
            {
                //Exception, index not valid
            }

        }

        /// <summary>
        /// Save the record into Dictionary
        /// </summary>
        /// <param name="result"></param>
        static void SaveRecordtoDictionary(IAsyncResult result)
        {
            WorkItem workItem = (WorkItem)result;

            if ((bool)workItem.OutputParameters)
            {
                dictionary.SaveRecord(false,
                                (ObservationRecord)workItem.InputParameters);
            }
            else
            {
                dictionary.SaveRecord(true,
                                (ObservationRecord)workItem.InputParameters);
            }
        }

        
    }
}


