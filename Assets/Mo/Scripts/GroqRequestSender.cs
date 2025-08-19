using UnityEngine;
using System.Collections;
using System.Text;
using UnityEngine.Networking;
using System;
using Meta.XR.MRUtilityKit;
using Mono.Cecil.Cil;
using NUnit.Framework;
using Unity.VisualScripting;
using Unity.XR.CompositionLayers.UIInteraction;
using UnityEngine.TextCore.Text;

public class GroqRequestSender : MonoBehaviour
{
    [SerializeField] private string apiUrl = "https://api.groq.com/openai/v1/chat/completions";
    [SerializeField] private string apiKey = "your_groq_api_key_here";
    [SerializeField] private string model = "llama3-70b-8192";
    [SerializeField] private string myObject = "bottle";

    [SerializeField, TextArea(3, 10)] private string userInput =
        "[INST]Write Chapter 1 of a 3-chapter creative story. " +
        "\nRules:\n- The story must be exactly 5 sentences long. " +
        "\n- Each chapter introduces a new object. Chapter 1 is about a bottle." +
        "\n- Output must be valid JSON with only these keys: " +
        "\n  {\n    \"chapter\": 1,\n    \"title\": \"string\",\n    \"story\": \"5 full sentences of narrative prose\",\n    \"image_prompt\": \"string describing the scene for image generation\",\n    \"ambient_audio\": \"one choice from: Storm, Airport, supermarket, forest, river, Submarine, space\"\n  }\n[/INST]";

    private string systemPromptTemplate = @"Analyze the following story addition and identify appropriate audio and lighting effects to enhance the scene. Return your analysis in the specified JSON format.

Story addition: ""{USER_INPUT}""

Based on the story content, determine:

1. Weather/Environmental Effects (use simple audio file names  like: wind, rain, fire, thunder, suspense, calm_music,forest_ambient, ocean_waves, breeze, bubbling)
2. Creature/Character Sounds (use simple audio file names like: growl, roar, footsteps, bird_chirp, wolf_howl,sheep,barking, owl, cat)
3. Emotional/Musical Tone (use simple audio file names like: tense_music, calm_music, dramatic_music, ambient_music, desert_music, happy_music, sad_music, scary_music, exciting_music)
4. Lighting Atmosphere (use simple color names like: red, blue, orange, white, yellow, pink  or hex codes like #FF0000)

Respond ONLY with this JSON structure (no markdown formatting):
{{
  ""chapters"": ""chapter"",
  ""titles"": ""title"",
  ""story"": ""story"",
  ""image_promtp"": ""image_prompt"",
  ""audio_effects"": {{
    ""environmental"": [""effect1"", ""effect2""],
    ""creatures"": [""sound1"", ""sound2""], 
    ""music_tone"": ""tone_description""
  }},
  ""lighting_effects"": {{
    ""intensity"": ""increase"",
    ""atmosphere"": ""atmosphere_description"",
    ""color_mood"": ""#FF6600""
  }},
  ""confidence"": ""high"",
  ""reasoning"": ""brief explanation of detected elements""
}}";

    [Serializable]
    private class ChatMessage
    {
        public string role;
        public string content;
    }

    [Serializable]
    private class ChatRequest
    {
        public string model;
        public ChatMessage[] messages;
        public float temperature;
        public int max_tokens;
    }

    [Serializable]
    private class GroqResponseWrapper
    {
        public Choice[] choices;
    }

    [Serializable]
    private class Choice
    {
        public Message message;
    }

    [Serializable]
    private class Message
    {
        public string content;
    }

    public string GetPrompt(int chapter, string myObject)
    {
        string str1, str2, newprompt = null;
        
        str1 = "[INST]You are a creative narrator guiding a 3-chapter improvisational story where everyday objects become fictional characters." +
"\n\nStory Rules:" +
"\n- The Chapters should be about 20–30 seconds when read aloud." +
"\n- The story has 3 chapters:" +
"\n   • Chapter 1: The first chosen object becomes the Hero, the main character of the adventure." +
"\n   • Chapter 2: A new everyday object becomes the Sidekick, who secretly does most of the work." +
"\n   • Chapter 3: A final everyday object becomes the Antagonist, the villain." +
"\n- Each chapter should introduce and focus on its new character while continuing the narrative." +
"\n- The story should end openly, leaving space for imagination." +
"\n- Write it in a casual, simple language, so that it is easy to understand." +
"\n\nChapter 1 Instructions:" +
"\n- Write from the perspective of a playful, imaginative narrator." +
"\n- Transform the object provided (" + myObject + ") into a larger-than-life Hero." +
"\n- Introduces the Character with a unique abilitie, its biggest flaw, and personality." +
"\n- Do not mention the sidekick yet!" +
"\n- The tone should feel whimsical, adventurous, and slightly improvised." +
                    "\n  {\n    \"chapter\": 1,\n    \"title\": \"string\",\n    \"story\": \"5 full sentences of narrative prose\",\n    \"image_prompt\": \"string describing the scene for image generation\",\n    \"ambient_audio\": \"one choice from: Storm, Airport, supermarket, forest, river, Submarine, space\"\n  }\n[/INST]";
        
        str2 = @"Analyze the following story addition and identify appropriate audio and lighting effects to enhance the scene. Return your analysis in the specified JSON format.

        Story addition: ""{USER_INPUT}""

        Based on the story content, determine:

        1. Weather/Environmental Effects (use simple audio file names  like: wind, rain, fire, thunder, suspense, calm_music,forest_ambient, ocean_waves, breeze, bubbling)
        2. Creature/Character Sounds (use simple audio file names like: growl, roar, footsteps, bird_chirp, wolf_howl,sheep,barking, owl, cat)
        3. Emotional/Musical Tone (use simple audio file names like: tense_music, calm_music, dramatic_music, ambient_music, desert_music, happy_music, sad_music, scary_music, exciting_music)
        4. Lighting Atmosphere (use simple color names like: red, blue, orange, white, yellow, pink  or hex codes like #FF0000)

        Respond ONLY with this JSON structure (no markdown formatting):
        {{
          ""chapters"": ""chapter"",
          ""titles"": ""title"",
          ""story"": ""story"",
          ""image_promtp"": ""image_prompt"",
          ""audio_effects"": {{
            ""environmental"": [""effect1"", ""effect2""],
            ""creatures"": [""sound1"", ""sound2""], 
            ""music_tone"": ""tone_description""
          }},
          ""lighting_effects"": {{
            ""intensity"": ""increase"",
            ""atmosphere"": ""atmosphere_description"",
            ""color_mood"": ""#FF6600""
          }},
          ""confidence"": ""high"",
          ""reasoning"": ""brief explanation of detected elements""
        }}";
        
        newprompt = str1 + str2;
        return (newprompt);
    }
    
    public void SendUserInput(string input, Action<string> callback, string myObject)
    {
        if (input == null)
            input = "\"Write the first of 3 chapters of a creative story which is 5 sentences long and were each chapter introduces a new object. the first chpter is about \" + myObject + \" Also creat a prompt which can be use for image generation, choose one ambient audio for that part of the story from this list of Storm, Airport, supermarket, forest, river, Submarine, space\";";
        userInput = input;
        Debug.Log("USER INPUT -> " + input);
        string prompt = systemPromptTemplate.Replace("{USER_INPUT}", userInput);
        StartCoroutine(SendRequestCoroutine(prompt, callback));
    }

    private IEnumerator SendRequestCoroutine(string prompt, Action<string> callback)
    {
        ChatRequest req = new ChatRequest
        {
            model = model,
            messages = new ChatMessage[]
            {
                new ChatMessage { role = "system", content = "You are a helpful assistant that outputs strictly in JSON format without markdown formatting." },
                new ChatMessage { role = "user", content = prompt }
            },
            temperature = 0.3f,
            max_tokens = 1000
        };

        string jsonBody = JsonUtility.ToJson(req);
        Debug.Log("Sending request to Groq API...");

        using (UnityWebRequest request = new UnityWebRequest(apiUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + apiKey);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string response = request.downloadHandler.text;
                string result = ExtractContentFromResponse(response);
                Debug.Log("API call successful!");
                callback?.Invoke(result);
            }
            else
            {
                Debug.LogError("Groq API Error: " + request.responseCode + " - " + request.error + "\n" + request.downloadHandler.text);
                callback?.Invoke(null);
            }
        }
    }

    private string ExtractContentFromResponse(string rawJson)
    {
        try
        {
            var wrapper = JsonUtility.FromJson<GroqResponseWrapper>(rawJson);
            string content = wrapper.choices[0].message.content;
            
            // Clean up any markdown formatting that might be present
            content = content.Replace("```json", "").Replace("```", "").Trim();
            
            return content;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to extract content from response: {e.Message}");
            return rawJson; // fallback if deserialization fails
        }
    }
}