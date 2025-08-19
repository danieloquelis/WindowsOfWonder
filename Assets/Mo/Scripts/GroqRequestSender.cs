using UnityEngine;
using System.Collections;
using System.Text;
using UnityEngine.Networking;
using System;
using System.Collections.Generic;
using System.Linq;

public class GroqRequestSender : MonoBehaviour
{
    [SerializeField] private string apiUrl = "https://api.groq.com/openai/v1/chat/completions";
    [SerializeField] private string apiKey = "your_groq_api_key_here";
    [SerializeField] private string model = "llama3-70b-8192";
    [SerializeField] private string myObject = "bottle";

    [SerializeField, TextArea(3, 10)] private string userInput =
        "[INST]Write Chapter 1 of a 3-chapter creative story. " +
        "\nRules:\n- The story must be exactly 1 sentences long. " +
        "\n- Each chapter introduces a new object." +
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

    public string GetPromptForUserEvaluation(string userStory, List<string> availableObjects = null)
    {
        return
            $@"Analyse this story given by the user of this game: {userStory} => And return a json structure with the following information:
            1. Small comment about the story highlighting the creativity and some elements on the story. Make sure to be concise having 10 seconds max. If the story is small or not creative, answer with a funny joke about it without being harsh.
            2. A message saying it's now the user's turn to pick an object for the AI. Something like 'Now it's YOUR turn to pick an object for me to create a story about. Let's see what you choose...' Adjust the tone based on user's story quality - be afraid if it was good, sarcastic if it wasn't.
            3. A prompt to generate an image based on the user story: {userStory}. Make sure it is optimised for flux image generation. Only 5 words maximum.

Respond ONLY with this JSON structure (no markdown formatting):
{{{{
  ""comment"": ""comment_text"",
  ""next_question"": ""next_question_text"",
  ""image_prompt"": ""image_prompt""
}}}}
";
    }

    public string GetPrompt(int chapter, string myObject, List<string> detectedObjects)
    {
        return $@"Write Chapter {chapter} of a creative story about {myObject}. This is a storytelling game where AI and User take turns.

        Your tasks:
        1. Create a creative story (5 sentences) about {myObject}
        2. Generate an image prompt for the story
        3. Pick the NEXT object from this list: {string.Join(", ", detectedObjects)} (must be different from {myObject})
        4. Challenge the user to create a story about your picked object

        Respond ONLY with this JSON structure (no markdown formatting):
        {{{{
          ""chapters"": ""{chapter}"",
          ""titles"": ""title for your story"",
          ""story"": ""your 5-sentence creative story about {myObject}"",
          ""image_prompt"": ""image generation prompt for your story"",
          ""audio_effects"": {{{{
            ""environmental"": [""effect1"", ""effect2""],
            ""creatures"": [""sound1"", ""sound2""], 
            ""music_tone"": ""tone_description""
          }}}},
          ""lighting_effects"": {{{{
            ""intensity"": ""increase"",
            ""atmosphere"": ""atmosphere_description"",
            ""color_mood"": ""#FF6600""
          }}}},
          ""confidence"": ""high"",
          ""next_question"": ""Now it is your turn, tell me a story about [YOUR_PICKED_OBJECT]. Let's see if you can beat me!"",
          ""selected_object_by_ai"": ""exact_object_name_from_list"",
          ""reasoning"": ""brief explanation""
        }}}}";
    }
    
    public void SendUserInput(string input, Action<string> callback)
    {
        userInput = input;
        Debug.Log("USER INPUT -> " + input);
        string prompt = systemPromptTemplate.Replace("{USER_INPUT}", userInput);
        StartCoroutine(SendRequestCoroutine(prompt, callback));
    }

    public void GenericLLMRequest(string prompt, Action<string> callback)
    {
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