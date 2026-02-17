import { ManifestSection } from "@umbraco-cms/backoffice/section";
import { ManifestWorkspace } from "@umbraco-cms/backoffice/workspace";
import {
  ManifestCollection,
  ManifestCollectionAction,
  ManifestCollectionView,
  UMB_COLLECTION_ALIAS_CONDITION,
} from "@umbraco-cms/backoffice/collection";
import SproutFormsListContext from "../workspaces/sproutFormsListContext";
import FormCollectionElement from "../collections/formCollection.element";
import FormsRepository from "../repositories/sproutFormsRepository";
import {
  ManifestEntityBulkAction,
  ManifestRepository,
} from "@umbraco-cms/backoffice/extension-registry";
import SproutFormsWorkspaceContext from "../workspaces/sproutFormsWorkspaceContext";
import { ManifestDashboard } from "@umbraco-cms/backoffice/dashboard";
import FormSubmissionsRepository from "../repositories/sproutFormSubmissionsRepository";
import FormSubmissionCollectionElement from "../collections/formSubmissionCollection.element";
import SproutFormSubmissionsListContext from "../workspaces/sproutFormSubmissionsContext";
import { ManifestModal } from "@umbraco-cms/backoffice/modal";
import CreateFormAction from "../actions/CreateFormAction";
import DeleteFormAction from "../actions/deleteFormAction";
import { SubmissionFieldManifest } from "./submissionFieldManifest";
import FileSubmissionFieldElement from "../workspaces/submissionEditors/fileSubmissionField.element";
import { FormFieldConfigManifest } from "./formFieldConfigManifest";
import KeyValuePairProperty from "../workspaces/fieldEditors/keyValuePairProperty.element";
import { ManifestPropertyEditorUi } from "@umbraco-cms/backoffice/property-editor";
import FormPickerElement from "../propertyEditors/formPickerElement";
import CreateFolderAction from "../actions/CreateFolderAction";
import FolderWorkspaceContext from "../workspaces/folderWorkspaceContext";
import SproutFormsDashboardElement from "../workspaces/sproutFormsDashboard.element";

const SproutFormSection: ManifestSection = {
  type: "section",
  alias: "sproutForms",
  name: "SproutForms",
  weight: 10,
  //element: SproutFormsListElement,
  meta: {
    label: "SproutForms",
    pathname: "sproutForms",
  },
};

const SproutFormsDashboard: ManifestDashboard = {
  type: "dashboard",
  alias: "sproutForms.dashboards.overview",
  name: "SproutForms Overview Dashboard",
  weight: 10,
  meta: {
    label: "Overview",
  },
  element: SproutFormsDashboardElement,
  conditions: [
    {
      alias: "Umb.Condition.SectionAlias",
      match: "sproutForms",
    },
  ],
};

/* const SproutFormsCommandCenterDashboard: ManifestDashboard = {
  type: "dashboard",
  alias: "sproutForms.dashboards.commandCenter",
  name: "SproutForms Command Center",
  weight: 5,
  meta: {
    label: "Command Center",
  },
  js: () => import("../workspaces/sproutFormsDashboard.element"),
  conditions: [
    {
      alias: "Umb.Condition.SectionAlias",
      match: "sproutForms",
    },
  ],
}; */

const SproutFormsWorkspace: ManifestWorkspace = {
  type: "workspace",
  kind: "routable",
  alias: "sproutForms.form.detail",
  name: "Sprout Forms Workspace",
  api: SproutFormsWorkspaceContext,
  meta: {
    entityType: "sprout-form",
  },
};

const SproutFolderWorkspace: ManifestWorkspace = {
  type: "workspace",
  kind: "routable",
  alias: "sproutForms.folder.detail",
  name: "Sprout Folder Workspace",
  api: FolderWorkspaceContext,
  meta: {
    entityType: "sprout-folder"
  }
}

const FormsCollection: ManifestCollection = {
  type: "collection",
  kind: "default",
  alias: "sproutForms.collections.forms",
  name: "Forms Collection",
  api: SproutFormsListContext,
  meta: {
    repositoryAlias: "sproutForms.repositories.forms",
  },
};

const FormsCollectionView: ManifestCollectionView = {
  type: "collectionView",
  alias: "sproutForms.collections.forms.overview",
  name: "Forms overview",
  js: FormCollectionElement,
  meta: {
    label: "Overview",
    icon: "icon-list",
    pathName: "overview",
  },
  conditions: [
    {
      alias: UMB_COLLECTION_ALIAS_CONDITION,
      match: "sproutForms.collections.forms",
    },
  ],
};

const FormRepository: ManifestRepository = {
  type: "repository",
  alias: "sproutForms.repositories.forms",
  name: "SproutForms Repository",
  api: FormsRepository,
};

const FormCollectionCreateAction: ManifestCollectionAction = {
  type: "collectionAction",
  kind: "button",
  name: "Form Collection Overview Create",
  alias: "sproutForms.collections.forms.createAction",
  api: CreateFormAction,
  meta: {
    label: "#general_create",
  },
  conditions: [
    {
      alias: UMB_COLLECTION_ALIAS_CONDITION,
      match: "sproutForms.collections.forms",
    },
  ],
};

const FolderCollectionCreateAction: ManifestCollectionAction = {
  type: "collectionAction",
  kind: "button",
  name: "Form Collection Overview Create Folder",
  alias: "sproutForms.collections.forms.createAction.folder",
  api: CreateFolderAction,
  meta: {
    label: "Create folder",
  },
  conditions: [
    {
      alias: UMB_COLLECTION_ALIAS_CONDITION,
      match: "sproutForms.collections.forms",
    },
  ],
};

const FormCollectionTrashBulkAction: ManifestEntityBulkAction = {
  type: "entityBulkAction",
  alias: "sproutForms.collections.forms.trashAction",
  name: "Form Collection Overview Trash",
  weight: 10,
  api: DeleteFormAction,
  forEntityTypes: ["sprout-form"],
  meta: {
    label: "Delete",
  },
  conditions: [
    {
      alias: UMB_COLLECTION_ALIAS_CONDITION,
      match: "sproutForms.collections.forms",
    },
  ],
};

const FormSubmissionsRepositoryManifest: ManifestRepository = {
  type: "repository",
  alias: "sproutForms.repositories.submissions",
  name: "SproutForms Repository",
  api: FormSubmissionsRepository,
};

const FormSubmissionsCollectionManifest: ManifestCollection = {
  type: "collection",
  kind: "default",
  alias: "sproutForms.collections.submissions",
  name: "Submissions Collection",
  api: SproutFormSubmissionsListContext,
  meta: {
    repositoryAlias: "sproutForms.repositories.submissions",
  },
};

const FormSubmissionsCollectionViewManifest: ManifestCollectionView = {
  type: "collectionView",
  alias: "sproutForms.collections.submissions.overview",
  name: "Submissions overview",
  js: FormSubmissionCollectionElement,
  meta: {
    label: "Overview",
    icon: "icon-list",
    pathName: "overview",
  },
  conditions: [
    {
      alias: UMB_COLLECTION_ALIAS_CONDITION,
      match: "sproutForms.collections.submissions",
    },
  ],
};

const FormSubmissionInfoModal: ManifestModal = {
  type: "modal",
  alias: "sproutForms.modal.submission.info",
  name: "SproutForms Submission Info Modal",
  js: () => import("../modals/formSubmissionInfoModal.element"),
};

const FileFieldSubmissionField: SubmissionFieldManifest = {
  type: "submissionField",
  alias: "sproutForms.submissionField.file",
  name: "File Field Submission Field",
  element: FileSubmissionFieldElement,
  fieldTypeAlias: "file",
};

const KeyValuePairFieldConfigProperty: FormFieldConfigManifest = {
  type: "formFieldConfig",
  alias: "sproutForms.fieldConfig.keyValuePair",
  name: "Key Value Pair Field Config Property",
  element: KeyValuePairProperty,
  propertyTypeAlias: "SproutForms.KeyValuePair",
};

const FormsPickerPropertyEditor: ManifestPropertyEditorUi = {
  type: 'propertyEditorUi',
  alias: 'sproutForms.propertyEditors.formPicker',
  name: 'SproutForms form picker',
  element: FormPickerElement,
  meta: {
    label: 'SproutForms form picker',
    icon: 'icon-list',
    group: 'common',
    propertyEditorSchemaAlias: "Umbraco.Plain.String"
  }
}

export const SproutFormManifests = [
  SproutFormSection,
  SproutFormsDashboard,
  SproutFormsWorkspace,
  SproutFolderWorkspace,
  FormsCollection,
  FormsCollectionView,
  FormRepository,
  FormCollectionCreateAction,
  FolderCollectionCreateAction,
  FormCollectionTrashBulkAction,
  FormSubmissionsRepositoryManifest,
  FormSubmissionsCollectionManifest,
  FormSubmissionsCollectionViewManifest,
  FormSubmissionInfoModal,
  FileFieldSubmissionField,
  KeyValuePairFieldConfigProperty,
  FormsPickerPropertyEditor
];
