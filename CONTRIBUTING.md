# Contributing

Thank you for considering contributing to this project!

There are many ways to contribute. For example:
* Answer question and give input on issues and pull requests.
* Add to the documentation.
* Add example data for testing purposes.
* Report bugs.
* Suggest functionality.
* Write code

To get started, open an issue here on GitHub explaining what you want to do. This allows others to see what contributions are under way and gives them a chance to give their opinion, come with insights or suggestions, and offer to help. Then fork the repository and make your changes. When done, create a pull request.

## Contributing to the documentation

Improve the documentation by correcting spelling or grammer, rewriting parts to be more readable, add more information, or add explanations for missing topics. 

Before opening an issue, search through issues to check if a similar issue already exists.

## Contributing example data

Example data is needed to test that the converter works. There might be a variety of options and possibilities in the test software that produces data with various quirks. Having examples of such data on hand is useful when testing the converter. If you have data that isn't confidential or does not reveal secrets about your process, it might be a valuable contribution to the project. Do keep in mind that censoring data might remove some of the valuable quirks from it.

Before opening an issue, to the best of your ability check if the example data you want to submit is different enough to offer value.

## Report bugs

Make sure you are using the latest version of the converter. Also try using the latest version of the WATS Client or NuGet package, it might be an API bug.

When reporting a bug include enough information for others to reproduce the issue, which might include the data that caused the bug to appear. If you can't do that, submit a ticket to `virinco@support.com` with the relevant data instead. We might still open an issue for it on GitHub, but we will find a way around the sensitive data.

Before opening an issue, search through issues to check if a similar issue already exists.

## Contribute suggestions

Suggest improvements to the documentation or code. Explain what the improvement is and why you want it.

Before opening an issue, search through issues to check if a similar issue already exists.

## Contributing code

This converter is written in C# using .NET Framework 4.6.2 in Visual Studio 2019. It is important the WATS Client is not installed when running the project, as the installed API will be used instead of the install NuGet package.

Please try to follow the existing code conventions and style, though we do recognize that it is all over the place at the moment.

### Compatability

This converter does not support WATS Client 5.1 or older, because that version uses .NET Framework 3.5. 

The converter should also seek to be backwards compatible with older versions of itself. Usually this means names of parameters and their default values should not be changed.

### Testing

The project uses the [MSTest framework](https://docs.microsoft.com/en-us/visualstudio/test/quick-start-test-driven-development-with-test-explorer) for testing the converter.

It is setup with two tests; one for setting up the API by registering the client to your WATS, and one for running the converter.

The values are hardcoded in the test, so you will need to change the values to reflect your setup.
* In SetupClient, fill in your information in the the call to RegisterClient.
* In TestConverter, fill in the path to the file you want to test the converter with. There are example files in the Examples folder.
* Run SetupClient once, then you can run TestConverter as many times as you want.

**DO NOT COMMIT THESE CHANGES!**

It might also be worthwhile to build the converter and install it on an actual WATS Client to test it.

## Submitting a Pull Request 

Submit a pull request from your fork with a clear description of what you have done and include the relevant issue number, as well as how your change has been tested.

We will look at the pull request at our earliest convenience.

If there is no activity for a prolonged period of time after feedback is given on a pull request, it will be closed.

## License

By contributing, you agree to license your contribution under the terms of the [LGPLv3](COPYING.LESSER) which is an extention of [GPLv3](COPYING).
