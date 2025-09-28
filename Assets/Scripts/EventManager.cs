using UnityEngine;
using System;
using System.Collections.Generic;


public static class EventManager
{
    
    public enum EventType
    {
        FruitMovementChanged,
        FruitBusyStateChanged,
        ValidResponseWindowChanged,
        ResponseRegistered,
        SliceAnimationStateChanged,
        GameStateChanged,
        FeedbackMessageStateChanged
    }

    //Dictionary to store event subscribers
    private static Dictionary<EventType, Action<GameObject, object>> eventDictionary =
        new Dictionary<EventType, Action<GameObject, object>>();

    public static void Subscribe(EventType eventType, Action<GameObject, object> listener)
    {
        if (!eventDictionary.ContainsKey(eventType))
        {
            eventDictionary[eventType] = null;
        }
        eventDictionary[eventType] += listener;
    }

    public static void Unsubscribe(EventType eventType, Action<GameObject, object> listener)
    {
        if (eventDictionary.ContainsKey(eventType) && eventDictionary[eventType] != null)
        {
            eventDictionary[eventType] -= listener;
        }
    }

    public static void TriggerEvent(EventType eventType, GameObject sender, object data)
    {
        if(eventDictionary.ContainsKey(eventType) && eventDictionary[eventType] != null)
        {
            eventDictionary[eventType](sender, data);
        }
    }

    public static void ClearEvents()
    {
        eventDictionary.Clear();
    }


}
