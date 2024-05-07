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
            public const string GIT_REPO_TEMPLATE_PATH = "templates";
            public const string GIT_REPO_TENANT_CONFIG_PATH = "flux/{0}-config.yaml";
            public const string GIT_REPO_TEAM_CONFIG_PATH = "flux/services/{0}.yaml";

            public const string POSTGRES_DB_KEY = "POSTGRES_DB";
            public const string TEAM_KEY = "team";
            public const string SERVICE_KEY = "service";
            public const string ENV_KEY = "environment";
            public const string RESOURCES_KEY = "resources";
            public const string SPEC_KEY = "spec";
            public const string VALUES_KEY = "values";
            public const string LABELS_KEY = "labels";
            public const string INGRESS_KEY = "ingress";
            public const string PREDEPLOY_KEY = "pre-deploy";
            public const string INFRA_KEY = "infra";
            public const string POSTGRESRESOURCEGROUPNAME_KEY = "postgresResourceGroupName";
            public const string POSTGRESSERVERNAME_KEY = "postgresServerName";
            public const string DEPENDS_ON_KEY = "dependsOn";

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
            public const string TEAM_SERVICE_DEPLOY_ENV_PATCH_FILE = "{0}/{1}/{2}/deploy/{3}/patch.yaml";
            public const string TEAM_SERVICE_INFRA_ENV_PATCH_FILE = "{0}/{1}/{2}/infra/{3}/patch.yaml";

            public const string TEMPLATE_VAR_SERVICE_NAME = "SERVICE_NAME";
            public const string TEMPLATE_VAR_ENVIRONMENT = "ENVIRONMENT";
            public const string TEMPLATE_VAR_ENV_INSTANCE = "ENV_INSTANCE";
            public const string TEMPLATE_VAR_DEPENDS_ON = "DEPENDS_ON";
            public const string TEMPLATE_VAR_PROGRAMME_NAME = "PROGRAMME_NAME";
            public const string TEMPLATE_VAR_TEAM_NAME = "TEAM_NAME";
            public const string TEMPLATE_VAR_SERVICE_CODE = "SERVICE_CODE";
            public const string TEMPLATE_VAR_VERSION = "VERSION";
            public const string TEMPLATE_VAR_VERSION_TAG = "VERSION_TAG";
            public const string TEMPLATE_VAR_MIGRATION_VERSION = "MIGRATION_VERSION";
            public const string TEMPLATE_VAR_MIGRATION_VERSION_TAG = "MIGRATION_VERSION_TAG";
            public const string TEMPLATE_VAR_PS_EXEC_VERSION = "PS_EXEC_VERSION";
            public const string TEMPLATE_VAR_DEFAULT_VERSION = "__ENVIRONMENT__adpinfcr__ENV_INSTANCE__401.azurecr.io/image/__SERVICE_NAME__:0.1.0#{\"$imagepolicy\":\"flux-config:__SERVICE_NAME__-__ENVIRONMENT__-0__ENV_INSTANCE__\"}";
            public const string TEMPLATE_VAR_DEFAULT_VERSION_TAG = "0.1.0#{\"$imagepolicy\":\"flux-config:__SERVICE_NAME__-__ENVIRONMENT__-0__ENV_INSTANCE__:tag\"}";
            public const string TEMPLATE_VAR_DEFAULT_MIGRATION_VERSION = "__SSV_PLATFORM_ACR__.azurecr.io/image/__SERVICE_NAME__-dbmigration:0.1.0#{\"$imagepolicy\":\"flux-config:__SERVICE_NAME__-dbmigration-__ENVIRONMENT__-0__ENV_INSTANCE__\"}";
            public const string TEMPLATE_VAR_DEFAULT_MIGRATION_VERSION_TAG = "0.1.0#{\"$imagepolicy\":\"flux-config:__SERVICE_NAME__-dbmigration-__ENVIRONMENT__-0__ENV_INSTANCE__:tag\"}";
            public const string TEMPLATE_VAR_PS_EXEC_DEFAULT_VERSION = "__SSV_PLATFORM_ACR__.azurecr.io/image/powershell-executor:1#{\"$imagepolicy\":\"flux-config:powershell-executor-__ENVIRONMENT__-0__ENV_INSTANCE__\"}";
            public const string TEMPLATE_IMAGEPOLICY_KEY = "#{\"$imagepolicy\":\"flux-config";
            public const string TEMPLATE_IMAGEPOLICY_KEY_VALUE = " # {\"$imagepolicy\": \"flux-config";
        }

        public static class Logger
        {
            public const string FLUX_TEAM_CONFIG_NOT_FOUND = "Flux team config not found for the team:'{TeamName}'.";
        }
    }
}
