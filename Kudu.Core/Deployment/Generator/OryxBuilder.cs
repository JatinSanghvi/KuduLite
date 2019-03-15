﻿using System;
using System.Text;
using System.Threading.Tasks;
using Kudu.Core.Helpers;
using Kudu.Contracts.Settings;

namespace Kudu.Core.Deployment.Generator
{
    public class OryxBuilder : ExternalCommandBuilder
    {
        public override string ProjectType => "Oryx-Build";

        public OryxBuilder(IEnvironment environment, IDeploymentSettingsManager settings, IBuildPropertyProvider propertyProvider, string sourcePath)
            : base(environment, settings, propertyProvider, sourcePath)
        {
        }

        public override Task Build(DeploymentContext context)
        {
            FileLogHelper.Log("In oryx build...");

            bool enableBuildInTemp = false;
            bool overrideOutFolder = false;
            string enable_node_zip = System.Environment.GetEnvironmentVariable("ENABLE_NODE_MODULES_ZIP");
            if (!string.IsNullOrEmpty(enable_node_zip) && enable_node_zip.Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                enableBuildInTemp = true;
            }

            string output_folder_override = System.Environment.GetEnvironmentVariable("OUTPUT_FOLDER_OVERRIDE");
            if (!string.IsNullOrEmpty(enable_node_zip) )
            {
                overrideOutFolder = true;
            }

            // Step 1: Run kudusync

            string kuduSyncCommand = string.Format("kudusync -v 50 -f {0} -t {1} -n {2} -p {3} -i \".git;.hg;.deployment;.deploy.sh\"",
                RepositoryPath,
                context.OutputPath,
                context.NextManifestFilePath,
                context.PreviousManifestFilePath
                );

            FileLogHelper.Log("Running KuduSync with  " + kuduSyncCommand);
            RunCommand(context, kuduSyncCommand, false, "Oryx-Build: Running kudu sync...");

            string framework = System.Environment.GetEnvironmentVariable("FRAMEWORK");
            string version = System.Environment.GetEnvironmentVariable("FRAMEWORK_VERSION");

            string oryxLanguage = "";
            string additionalOptions = "";
            bool runOryxBuild = false;

            if (framework.StartsWith("NODE"))
            {
                oryxLanguage = "nodejs";
                runOryxBuild = true;
            }
            else if (framework.StartsWith("PYTHON"))
            {
                oryxLanguage = "python";
                runOryxBuild = true;
                string virtualEnvName = "antenv";

                if (version.StartsWith("3.6"))
                {
                    virtualEnvName = "antenv3.6";
                }
                else if (version.StartsWith("2.7"))
                {
                    virtualEnvName = "antenv2.7";
                }

                additionalOptions = string.Format("-p virtualenv_name={0}", virtualEnvName);
            }
            else if (framework.StartsWith("DOTNETCORE"))
            {
                oryxLanguage = "dotnet";
                runOryxBuild = true;
            }

            string outputPath = context.OutputPath;
            if (overrideOutFolder)
            {
                outputPath = output_folder_override;
            }

            if (enableBuildInTemp)
            {
                additionalOptions+= string.Format(" -i {0}", context.BuildTempPath);
            }

            if (runOryxBuild)
            {
                string oryxBuildCommand = string.Format("oryx build {0} -o {1} -l {2} --language-version {3} {4}",
                    context.OutputPath,
                    outputPath,
                    oryxLanguage,
                    version,
                    additionalOptions);

                RunCommand(context, oryxBuildCommand, false, "Running oryx build...");
            }

            if (overrideOutFolder)
            {
                string textToWrite = string.Format("node_modules.zip:{0}/node_modules", context.OutputPath);
                string pathToWrite = string.Format("{0}/node_modules.txt", output_folder_override);
                File.WriteAllText(pathToWrite, textToWrite);
            }

            return Task.CompletedTask;
        }

        //public override void PostBuild(DeploymentContext context)
        //{
        //    // no-op
        //    context.Logger.Log($"Skipping post build. Project type: {ProjectType}");
        //    FileLogHelper.Log("Completed PostBuild oryx....");
        //}
    }
}
