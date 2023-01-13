using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using UnityEngine;

public enum AuthState
{
    Initialized,
    Authenticating,
    Authenticated,
    Error,
    TimedOut
}

public static class Authentication
{
    private static AuthState AuthenticationState { get; set; } = AuthState.Initialized;

    public static async Task<AuthState> Authenticate(string profile, int tries = 5)
    {
        if (AuthenticationState == AuthState.Authenticated)
        {
            return AuthenticationState;
        }

        if (AuthenticationState == AuthState.Authenticating)
        {
            return await Authenticating();
        }
        
        var options = new InitializationOptions();
        options.SetProfile(profile);
        await UnityServices.InitializeAsync(options);
        await SignInAnonymouslyAsync(tries);

        return AuthenticationState;
    }

    private static async Task<AuthState> Authenticating()
    {
        while (AuthenticationState is AuthState.Authenticating or AuthState.Initialized)
        {
            await Task.Delay(200);
        }

        return AuthenticationState;
    }

    private static async Task SignInAnonymouslyAsync(int maxRetries)
    {
        AuthenticationState = AuthState.Authenticating;

        var tries = 0;

        while (AuthenticationState == AuthState.Authenticating && tries < maxRetries)
        {
            try
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

                if (AuthenticationService.Instance.IsSignedIn && AuthenticationService.Instance.IsAuthorized)
                {
                    AuthenticationState = AuthState.Authenticated;
                    break;
                }
            }
            catch (AuthenticationException ex)
            {
                // todo
                // Compare error code to AuthenticationErrorCodes
                // Notify the player with the proper error message
                Debug.LogError(ex);
                AuthenticationState = AuthState.Error;
            }
            catch (RequestFailedException exception)
            {
                // todo
                // Compare error code to CommonErrorCodes
                // Notify the player with the proper error message
                Debug.LogError(exception);
                AuthenticationState = AuthState.Error;
            }

            tries++;

            await Task.Delay(1000);
        }

        if (AuthenticationState != AuthState.Authenticated)
        {
            Debug.LogWarning($"Player was not signed in successfully after {tries} attempts");
            AuthenticationState = AuthState.TimedOut;
        }
    }
}