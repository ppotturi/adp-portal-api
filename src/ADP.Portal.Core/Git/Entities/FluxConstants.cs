namespace ADP.Portal.Core.Git.Entities
{
    public static class FluxConstants
    {
        public const string GIT_REPO_TEMPLATE_PATH = "flux/templates";
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

        public const string PROGRAMME_FOLDER = "flux/templates/programme";
        public const string SERVICE_FOLDER = "flux/templates/programme/team/service";
        public const string TEAM_ENV_FOLDER = "flux/templates/programme/team/environment";
        public const string SERVICE_PRE_DEPLOY_FOLDER = "flux/templates/programme/team/service/pre-deploy";

        public const string PRE_DEPLOY_KUSTOMIZE_FILE = "flux/templates/programme/team/service/pre-deploy-kustomize.yaml";
        public const string TEAM_ENV_KUSTOMIZATION_FILE = "flux/templates/programme/team/environment/kustomization.yaml";
        public const string TEAM_SERVICE_ENV_PATCH_FILE = "{0}/{1}/{2}/deploy/{3}/patch.yaml";

        public const string TEMPLATE_VAR_SERVICE_NAME = "SERVICE_NAME";
        public const string TEMPLATE_VAR_ENVIRONMENT = "ENVIRONMENT";
        public const string TEMPLATE_VAR_ENV_INSTANCE = "ENV_INSTANCE";
        public const string TEMPLATE_VAR_DEPENDS_ON = "DEPENDS_ON";
        public const string TEMPLATE_VAR_PROGRAMME_NAME = "PROGRAMME_NAME";
        public const string TEMPLATE_VAR_TEAM_NAME = "TEAM_NAME";
        public const string TEMPLATE_VAR_SERVICE_CODE = "SERVICE_CODE";
        public const string TEMPLATE_VAR_VERSION = "VERSION";
        public const string TEMPLATE_VAR_DEFAULT_VERSION = "0.1.0";
    }
}
