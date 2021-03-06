﻿using UnityEngine;
using System.Collections;
using Zenject;
using UniRx;
using System;

//Simple, non-invasive Kernel class based on Zenject.SceneCompositionRoot
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using ModestTree.Util;

public partial class Kernel : SceneContext {

    Service.Events.IEventsService eventService;
    DisposableManager dManager;

    static bool loadingKernelScene = false;
    public static bool applicationQuitting = false;

    public static ReactiveProperty<Kernel> InstanceProperty = new ReactiveProperty<Kernel>();
    public static Kernel Instance {
        get{

            // Added !applicationQuitting into the if check to make sure that the kernel scene isn't loaded OnApplicationQuit, when it became null
            // Seems like OnApplicationQuit is sometimes wrongly called, causing a reload of the Kernel Scene.
            // Loading the Kernel Scene should only happen from inside the editor, if the kernel scene was not initially loaded
            if (InstanceProperty.Value == null && !loadingKernelScene && SceneManager.GetActiveScene().name != "Kernel" && !applicationQuitting){

                System.Diagnostics.StackFrame frame = new System.Diagnostics.StackFrame(1);
                var method = frame.GetMethod();
                var type = method.DeclaringType;
                var name = method.Name;
                Debug.Log("kernel is null! Calling script:"+ frame.GetFileName() +" in scene: "+ SceneManager.GetActiveScene().name + " calling method name: "+ name);

                loadingKernelScene = true;
                SceneManager.LoadScene("Kernel");
            }

            return InstanceProperty.Value;
        }
        private set{
            InstanceProperty.Value = value;
        }
    }

    /// <summary>
    /// Inject the specified injectable to Kernel Container, thus triggering DependencyInjection
    /// </summary>
    /// <param name="injectable">Injectable.</param>
    public void Inject(object injectable)
    {
        Container.Inject(injectable);
    }

    new void Awake(){
        Instance = this;
        base.Awake();
    }

    void Start(){
        //Resolve disposableManager
        dManager = Container.Resolve<DisposableManager>();
        //Let the program know, that Kernel has finished loading
        eventService = Container.Resolve<Service.Events.IEventsService>();
        eventService.Publish(new Events.KernelReadyEvent());
    }

    void OnApplicationFocus(bool hasFocus) {
        if(eventService != null) {
            if (hasFocus) {
                eventService.Publish(new Events.OnApplicationFocus());
            } else {
                eventService.Publish(new Events.OnApplicationLostFocus());
            }
            
        }
    }

    private void OnApplicationQuit() {
        SetOnApplicationQuitSettings();

        if (eventService != null) {
            eventService.Publish(new Events.OnApplicationQuit());
        }  
    }

    // since there seems to be no defined way to hit the very first OnApplicationQuit
    // set settings to discard dispose must be called from somewhere else as well 
    // (e.g. gameObject's OnApplicationQuit) That is ugly....
    //Edit: This is now called by the GameService right before the application is told to quit
    public void SetOnApplicationQuitSettings() {
        if (!applicationQuitting) {
            applicationQuitting = true;
            dManager.SkipDispose(true);
        }
    }

    /*
    void OnApplicationPause(bool pauseStatus) {
        if(eventService != null) {
        }
    }
    */
}