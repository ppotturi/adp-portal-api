namespace ADP.Portal.Core.Git.Entities
{
    public static class Constants
    {
        public static class GitRepo
        {
            public const string TEAM_REPO_CONFIG = "TeamGitRepo";
            public const string TEAM_FLUX_SERVICES_CONFIG = "FluxServicesGitRepo";
            public const string TEAM_FLUX_TEMPLATES_CONFIG = "FluxTemplatesGitRepo";

        }

        public static class Flux
        {
            public static class Services
            {
                public const string TEAM_SERVICE_DEPLOY_ENV_PATCH_FILE = "services/{0}/{1}/{2}/deploy/{3}/patch.yaml";
                public const string TEAM_SERVICE_INFRA_ENV_PATCH_FILE = "services/{0}/{1}/{2}/infra/{3}/patch.yaml";
                public const string TEAM_SERVICE_ENV_KUSTOMIZATION_FILE = "services/{0}/{1}/{2}/kustomization.yaml";
                public const string TEAM_ENV_BASE_KUSTOMIZATION_FILE = "services/environments/{0}/base/kustomization.yaml";
            }

            public static class Templates
            {
                public const string GIT_REPO_TEMPLATE_PATH = "templates";
                public const string GIT_REPO_TENANT_CONFIG_PATH = "flux/{0}-config.yaml";
                public const string GIT_REPO_TEAM_CONFIG_PATH = "flux/services/{0}.yaml";

                public const string PROGRAMME_FOLDER = GIT_REPO_TEMPLATE_PATH + "/programme";
                public const string SERVICE_FOLDER = PROGRAMME_FOLDER + "/team/service";
                public const string TEAM_ENV_FOLDER = PROGRAMME_FOLDER + "/team/environment";
                public const string SERVICE_PRE_DEPLOY_FOLDER = PROGRAMME_FOLDER + "/team/service/pre-deploy";
                public const string SERVICE_INFRA_FOLDER = PROGRAMME_FOLDER + "/team/service/infra";

                public const string PRE_DEPLOY_KUSTOMIZE_FILE = PROGRAMME_FOLDER + "/team/service/pre-deploy-kustomize.yaml";
                public const string DEPLOY_KUSTOMIZE_FILE = PROGRAMME_FOLDER + "/team/service/deploy-kustomize.yaml";
                public const string INFRA_KUSTOMIZE_FILE = PROGRAMME_FOLDER + "/team/service/infra-kustomize.yaml";
                public const string TEAM_ENV_KUSTOMIZATION_FILE = PROGRAMME_FOLDER + "/team/environment/kustomization.yaml";
                public const string TEAM_SERVICE_KUSTOMIZATION_FILE = PROGRAMME_FOLDER + "/team/service/kustomization.yaml";

                public const string POSTGRES_DB_KEY = "POSTGRES_DB";
                public const string TEAM_KEY = "team";
                public const string SERVICE_KEY = "service";
                public const string ENV_KEY = "environment";
                public const string RESOURCES_KEY = "resources";
                public const string SPEC_KEY = "spec";
                public const string VALUES_KEY = "values";
                public const string LABELS_KEY = "labels";
                public const string INGRESS_KEY = "ingress";
                public const string INGRESS_ENDPOINT_TOKEN_KEY = "INGRESS_ENDPOINT";
                public const string PREDEPLOY_KEY = "pre-deploy";
                public const string INFRA_KEY = "infra";
                public const string POSTGRESRESOURCEGROUPNAME_KEY = "postgresResourceGroupName";
                public const string POSTGRESSERVERNAME_KEY = "postgresServerName";
                public const string DEPENDS_ON_KEY = "dependsOn";

                public const string SERVICE_NAME_TOKEN = "SERVICE_NAME";
                public const string ENVIRONMENT_TOKEN = "ENVIRONMENT";
                public const string ENV_INSTANCE_TOKEN = "ENV_INSTANCE";
                public const string DEPENDS_ON_TOKEN = "DEPENDS_ON";
                public const string PROGRAMME_NAME_TOKEN = "PROGRAMME_NAME";
                public const string TEAM_NAME_TOKEN = "TEAM_NAME";
                public const string SERVICE_CODE_TOKEN = "SERVICE_CODE";
                public const string VERSION_TOKEN = "VERSION";
                public const string VERSION_TAG_TOKEN = "VERSION_TAG";
                public const string MIGRATION_VERSION_TOKEN = "MIGRATION_VERSION";
                public const string MIGRATION_VERSION_TAG_TOKEN = "MIGRATION_VERSION_TAG";
                public const string PS_EXEC_VERSION_TOKEN = "PS_EXEC_VERSION";

                public const string DEFAULT_VERSION_TOKEN_VALUE = "__ENVIRONMENT__adpinfcr__ENV_INSTANCE__401.azurecr.io/image/__SERVICE_NAME__:0.1.0#{\"$imagepolicy\":\"flux-config:__SERVICE_NAME__-__ENVIRONMENT__-0__ENV_INSTANCE__\"}";
                public const string DEFAULT_VERSION_TAG_TOKEN_VALUE = "0.1.0#{\"$imagepolicy\":\"flux-config:__SERVICE_NAME__-__ENVIRONMENT__-0__ENV_INSTANCE__:tag\"}";
                public const string DEFAULT_MIGRATION_VERSION_TOKEN_VALUE = "__SSV_PLATFORM_ACR__.azurecr.io/image/__SERVICE_NAME__-dbmigration:0.1.0#{\"$imagepolicy\":\"flux-config:__SERVICE_NAME__-dbmigration-__ENVIRONMENT__-0__ENV_INSTANCE__\"}";
                public const string DEFAULT_MIGRATION_VERSION_TAG_TOKEN_VALUE = "0.1.0#{\"$imagepolicy\":\"flux-config:__SERVICE_NAME__-dbmigration-__ENVIRONMENT__-0__ENV_INSTANCE__:tag\"}";
                public const string PS_EXEC_DEFAULT_VERSION_TOKEN_VALUE = "__SSV_PLATFORM_ACR__.azurecr.io/image/powershell-executor:1#{\"$imagepolicy\":\"flux-config:powershell-executor-__ENVIRONMENT__-0__ENV_INSTANCE__\"}";
                public const string IMAGEPOLICY_KEY = "#{\"$imagepolicy\":\"flux-config";
                public const string IMAGEPOLICY_KEY_VALUE = " # {\"$imagepolicy\": \"flux-config";

            }
        }

        public static class Logger
        {
            public const string FLUX_TEAM_CONFIG_NOT_FOUND = "Flux team config not found for the team:'{TeamName}'.";
        }
    }
}
