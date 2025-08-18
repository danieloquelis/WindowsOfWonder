using UnityEngine;
using System;

[System.Serializable]
public class GroqResponseParser
{
    public string story;
    public string image_prompt;
    public AudioEffects audio_effects;
    public LightingEffects lighting_effects;
    public string confidence;
    public string reasoning;

    public GroqResponseParser(string json)
    {
        try
        {
            var temp = JsonUtility.FromJson<GroqResponseParser>(json);
            audio_effects = temp.audio_effects;
            lighting_effects = temp.lighting_effects;
            confidence = temp.confidence;
            reasoning = temp.reasoning;
            story = temp.story;
            image_prompt = temp.image_prompt;
            
            Debug.Log($"Parsed JSON successfully. Confidence: {confidence}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to parse JSON: {e.Message}");
            Debug.LogError($"JSON was: {json}");
            
            // Initialize with empty values to prevent null reference errors
            audio_effects = new AudioEffects();
            lighting_effects = new LightingEffects();
            confidence = "low";
            reasoning = "Failed to parse response";
        }
    }

    [System.Serializable]
    public class AudioEffects 
    { 
        public string[] environmental = new string[0];
        public string[] creatures = new string[0];
        public string music_tone = "";
    }
    
    [System.Serializable]
    public class LightingEffects 
    { 
        public string intensity = "";
        public string atmosphere = "";
        public string color_mood = "";
    }

    // Audio Methods
    public string[] GetEnvironmentalSounds() => audio_effects?.environmental ?? new string[0];
    public string[] GetCreatureSounds() => audio_effects?.creatures ?? new string[0];
    public string GetMusicTone() => audio_effects?.music_tone ?? "";
    
    // Lighting Methods
    public string GetLightingBrightness() => lighting_effects?.intensity ?? "";
    public string[] GetLightingColors() 
    {
        // Convert color_mood to an array for compatibility
        if (string.IsNullOrEmpty(lighting_effects?.color_mood))
            return new string[0];
        
        return new string[] { lighting_effects.color_mood };
    }
    
    // Additional helper methods
    public string GetLightingAtmosphere() => lighting_effects?.atmosphere ?? "";
    public string GetConfidence() => confidence ?? "unknown";
    public string GetReasoning() => reasoning ?? "";
    
    public string GetStory() => story ?? "";
    public string GetImagePrompt() => image_prompt ?? "";
}