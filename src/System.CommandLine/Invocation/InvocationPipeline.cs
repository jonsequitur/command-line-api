// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using System.Threading.Tasks;

namespace System.CommandLine.Invocation
{
    internal static class InvocationPipeline
    {
        internal static async Task<int> InvokeAsync(ParseResult parseResult, CancellationToken cancellationToken)
        {
            ProcessTerminationHandler? terminationHandler = null;
            using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            try
            {
                int exitCode = 0;

                if (parseResult.PreActions is not null)
                {
                    for (int i = 0; i < parseResult.PreActions.Count; i++)
                    {
                        var action = parseResult.PreActions[i];
                        int preActionResult;

                        switch (action)
                        {
                            case SynchronousCommandLineAction syncAction:
                                preActionResult = syncAction.Invoke(parseResult);
                                break;
                            case AsynchronousCommandLineAction asyncAction:
                                preActionResult = await asyncAction.InvokeAsync(parseResult, cts.Token);
                                break;
                            default:
                                preActionResult = 0;
                                break;
                        }

                        if (exitCode == 0)
                        {
                            exitCode = preActionResult;
                        }
                    }
                }

                if (parseResult.Action is null)
                {
                    return exitCode != 0 ? exitCode : ReturnCodeForMissingAction(parseResult);
                }

                int actionResult;

                switch (parseResult.Action)
                {
                    case SynchronousCommandLineAction syncAction:
                        actionResult = syncAction.Invoke(parseResult);
                        break;

                    case AsynchronousCommandLineAction asyncAction:
                        var startedInvocation = asyncAction.InvokeAsync(parseResult, cts.Token);

                        var timeout = parseResult.InvocationConfiguration.ProcessTerminationTimeout;

                        if (timeout.HasValue)
                        {
                            terminationHandler = new(cts, startedInvocation, timeout.Value);
                        }

                        if (terminationHandler is null)
                        {
                            actionResult = await startedInvocation;
                        }
                        else
                        {
                            // Handlers may not implement cancellation.
                            // In such cases, when CancelOnProcessTermination is configured and user presses Ctrl+C,
                            // ProcessTerminationCompletionSource completes first, with the result equal to native exit code for given signal.
                            Task<int> firstCompletedTask = await Task.WhenAny(startedInvocation, terminationHandler.ProcessTerminationCompletionSource.Task);
                            actionResult = await firstCompletedTask; // return the result or propagate the exception
                        }
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(parseResult.Action));
                }

                return exitCode != 0 ? exitCode : actionResult;
            }
            catch (Exception ex) when (parseResult.InvocationConfiguration.EnableDefaultExceptionHandler)
            {
                return DefaultExceptionHandler(ex, parseResult);
            }
            finally
            {
                terminationHandler?.Dispose();
            }
        }

        internal static int Invoke(ParseResult parseResult)
        {
            try
            {
                int exitCode = 0;

                if (parseResult.PreActions is not null)
                {
#if DEBUG
                    for (var i = 0; i < parseResult.PreActions.Count; i++)
                    {
                        var action = parseResult.PreActions[i];

                        if (action is not SynchronousCommandLineAction)
                        {
                            parseResult.InvocationConfiguration.EnableDefaultExceptionHandler = false;
                            throw new Exception(
                                $"This should not happen. An instance of {nameof(AsynchronousCommandLineAction)} ({action}) was called within {nameof(InvocationPipeline)}.{nameof(Invoke)}. This is supposed to be detected earlier resulting in a call to {nameof(InvocationPipeline)}{nameof(InvokeAsync)}");
                        }
                    }
#endif

                    for (var i = 0; i < parseResult.PreActions.Count; i++)
                    {
                        if (parseResult.PreActions[i] is SynchronousCommandLineAction syncPreAction)
                        {
                            int preActionResult = syncPreAction.Invoke(parseResult);
                            if (exitCode == 0)
                            {
                                exitCode = preActionResult;
                            }
                        }
                    }
                }

                switch (parseResult.Action)
                {
                    case null:
                        return exitCode != 0 ? exitCode : ReturnCodeForMissingAction(parseResult);

                    case SynchronousCommandLineAction syncAction:
                        int actionResult = syncAction.Invoke(parseResult);
                        return exitCode != 0 ? exitCode : actionResult;

                    default:
                        throw new InvalidOperationException($"{nameof(AsynchronousCommandLineAction)} called within non-async invocation.");
                }
            }
            catch (Exception ex) when (parseResult.InvocationConfiguration.EnableDefaultExceptionHandler)
            {
                return DefaultExceptionHandler(ex, parseResult);
            }
        }

        private static int DefaultExceptionHandler(Exception exception, ParseResult parseResult)
        {
            if (exception is not OperationCanceledException)
            {
                ConsoleHelpers.ResetTerminalForegroundColor();
                ConsoleHelpers.SetTerminalForegroundRed();

                var error = parseResult.InvocationConfiguration.Error;

                error.Write(LocalizationResources.ExceptionHandlerHeader());
                error.WriteLine(exception.ToString());

                ConsoleHelpers.ResetTerminalForegroundColor();
            }
            return 1;
        }

        private static int ReturnCodeForMissingAction(ParseResult parseResult)
        {
            if (parseResult.Errors.Count > 0)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }
    }
}
