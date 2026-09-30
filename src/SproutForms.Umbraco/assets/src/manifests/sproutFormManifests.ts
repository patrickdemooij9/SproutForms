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
import FormSubmissionsRepository, { TrashedFormSubmissionsRepository } from "../repositories/sproutFormSubmissionsRepository";
import {
  DeleteSubmissionPermanentlyEntityAction,
  DeleteSubmissionsPermanentlyBulkAction,
  EmptySubmissionsRecycleBinAction,
  RestoreSubmissionEntityAction,
  RestoreSubmissionsBulkAction,
  TrashSubmissionEntityAction,
  TrashSubmissionsBulkAction,
} from "../actions/submissionRecycleBinActions";
import { SUBMISSIONS_COLLECTION_ALIAS, SUBMISSIONS_RECYCLE_BIN_COLLECTION_ALIAS } from "../collections/submissionCollectionAliases";
import FormSubmissionCollectionElement from "../collections/formSubmissionCollection.element";
import SproutFormSubmissionsListContext from "../workspaces/sproutFormSubmissionsContext";
import { ManifestModal } from "@umbraco-cms/backoffice/modal";
import CreateFormAction from "../actions/CreateFormAction";
import { FORM_TYPE_PICKER_MODAL_ALIAS } from "../modals/formTypePickerModal.alias";
import { FORM_ROLLBACK_MODAL_ALIAS } from "../modals/formRollbackModal.token";
import { WORKFLOW_TEMPLATE_MODAL_ALIAS } from "../modals/workflowTemplateModal.token";
import { TrashFormEntityAction, TrashFormsBulkAction } from "../actions/trashFormActions";
import {
  DeleteFormPermanentlyEntityAction,
  DeleteFormsPermanentlyBulkAction,
  EmptyRecycleBinAction,
  RestoreFormEntityAction,
  RestoreFormsBulkAction,
} from "../actions/recycleBinActions";
import RecycleBinWorkspaceContext, { RECYCLE_BIN_ENTITY_TYPE } from "../workspaces/recycleBinWorkspaceContext";
import RecycleBinCollectionContext, { RECYCLE_BIN_COLLECTION_ALIAS } from "../workspaces/recycleBinCollectionContext";
import RecycleBinCollectionElement from "../collections/recycleBinCollection.element";
import RecycleBinRepository from "../repositories/recycleBinRepository";
import { SUBMISSION_ENTITY_TYPE, TRASHED_FORM_ENTITY_TYPE, TRASHED_SUBMISSION_ENTITY_TYPE } from "../models";
import { ManifestEntityAction } from "@umbraco-cms/backoffice/entity-action";
import { SubmissionFieldManifest } from "./submissionFieldManifest";
import FileSubmissionFieldElement from "../workspaces/submissionEditors/fileSubmissionField.element";
import RepeaterSubmissionFieldElement from "../workspaces/submissionEditors/repeaterSubmissionField.element";
import { FormFieldConfigManifest } from "./formFieldConfigManifest";
import KeyValuePairProperty from "../workspaces/fieldEditors/keyValuePairProperty.element";
import FieldOptionPickerProperty from "../workspaces/fieldEditors/fieldOptionPickerProperty.element";
import { ManifestPropertyEditorUi } from "@umbraco-cms/backoffice/property-editor";
import FormPickerElement from "../propertyEditors/formPickerElement";
import TokenAutocompleteTextareaElement from "../propertyEditors/tokenAutocompleteTextarea.element";
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
  api: TrashFormsBulkAction,
  forEntityTypes: ["sprout-form"],
  meta: {
    label: "Move to recycle bin",
  },
  conditions: [
    {
      alias: UMB_COLLECTION_ALIAS_CONDITION,
      match: "sproutForms.collections.forms",
    },
  ],
};

const FormTrashEntityAction: ManifestEntityAction = {
  type: "entityAction",
  kind: "default",
  alias: "sproutForms.entityActions.form.trash",
  name: "Move Form To Recycle Bin",
  weight: 100,
  api: TrashFormEntityAction,
  forEntityTypes: ["sprout-form"],
  meta: {
    icon: "icon-trash",
    label: "Move to recycle bin",
  },
};

const RecycleBinWorkspace: ManifestWorkspace = {
  type: "workspace",
  kind: "routable",
  alias: "sproutForms.recycleBin",
  name: "SproutForms Recycle Bin Workspace",
  api: RecycleBinWorkspaceContext,
  meta: {
    entityType: RECYCLE_BIN_ENTITY_TYPE,
  },
};

const RecycleBinCollection: ManifestCollection = {
  type: "collection",
  kind: "default",
  alias: RECYCLE_BIN_COLLECTION_ALIAS,
  name: "SproutForms Recycle Bin Collection",
  api: RecycleBinCollectionContext,
  meta: {
    repositoryAlias: "sproutForms.repositories.recycleBin",
  },
};

const RecycleBinCollectionView: ManifestCollectionView = {
  type: "collectionView",
  alias: `${RECYCLE_BIN_COLLECTION_ALIAS}.overview`,
  name: "SproutForms Recycle Bin Overview",
  element: RecycleBinCollectionElement,
  meta: {
    label: "Overview",
    icon: "icon-list",
    pathName: "overview",
  },
  conditions: [
    {
      alias: UMB_COLLECTION_ALIAS_CONDITION,
      match: RECYCLE_BIN_COLLECTION_ALIAS,
    },
  ],
};

const RecycleBinRepositoryManifest: ManifestRepository = {
  type: "repository",
  alias: "sproutForms.repositories.recycleBin",
  name: "SproutForms Recycle Bin Repository",
  api: RecycleBinRepository,
};

const RecycleBinEmptyAction: ManifestCollectionAction = {
  type: "collectionAction",
  kind: "button",
  alias: `${RECYCLE_BIN_COLLECTION_ALIAS}.emptyAction`,
  name: "Empty SproutForms Recycle Bin",
  api: EmptyRecycleBinAction,
  meta: {
    label: "Empty recycle bin",
  },
  conditions: [
    {
      alias: UMB_COLLECTION_ALIAS_CONDITION,
      match: RECYCLE_BIN_COLLECTION_ALIAS,
    },
  ],
};

const RecycleBinRestoreBulkAction: ManifestEntityBulkAction = {
  type: "entityBulkAction",
  alias: `${RECYCLE_BIN_COLLECTION_ALIAS}.restoreAction`,
  name: "Restore Forms From Recycle Bin",
  weight: 20,
  api: RestoreFormsBulkAction,
  forEntityTypes: [TRASHED_FORM_ENTITY_TYPE],
  meta: {
    label: "Restore",
  },
  conditions: [
    {
      alias: UMB_COLLECTION_ALIAS_CONDITION,
      match: RECYCLE_BIN_COLLECTION_ALIAS,
    },
  ],
};

const RecycleBinDeleteBulkAction: ManifestEntityBulkAction = {
  type: "entityBulkAction",
  alias: `${RECYCLE_BIN_COLLECTION_ALIAS}.deleteAction`,
  name: "Delete Forms Permanently",
  weight: 10,
  api: DeleteFormsPermanentlyBulkAction,
  forEntityTypes: [TRASHED_FORM_ENTITY_TYPE],
  meta: {
    label: "Delete permanently",
  },
  conditions: [
    {
      alias: UMB_COLLECTION_ALIAS_CONDITION,
      match: RECYCLE_BIN_COLLECTION_ALIAS,
    },
  ],
};

const TrashedFormRestoreEntityAction: ManifestEntityAction = {
  type: "entityAction",
  kind: "default",
  alias: "sproutForms.entityActions.trashedForm.restore",
  name: "Restore Form From Recycle Bin",
  weight: 200,
  api: RestoreFormEntityAction,
  forEntityTypes: [TRASHED_FORM_ENTITY_TYPE],
  meta: {
    icon: "icon-undo",
    label: "Restore",
  },
};

const TrashedFormDeleteEntityAction: ManifestEntityAction = {
  type: "entityAction",
  kind: "default",
  alias: "sproutForms.entityActions.trashedForm.delete",
  name: "Delete Form Permanently",
  weight: 100,
  api: DeleteFormPermanentlyEntityAction,
  forEntityTypes: [TRASHED_FORM_ENTITY_TYPE],
  meta: {
    icon: "icon-trash",
    label: "Delete permanently",
  },
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
  alias: SUBMISSIONS_COLLECTION_ALIAS,
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
      match: SUBMISSIONS_COLLECTION_ALIAS,
    },
  ],
};

const SubmissionTrashBulkAction: ManifestEntityBulkAction = {
  type: "entityBulkAction",
  alias: `${SUBMISSIONS_COLLECTION_ALIAS}.trashAction`,
  name: "Move Submissions To Recycle Bin",
  weight: 10,
  api: TrashSubmissionsBulkAction,
  forEntityTypes: [SUBMISSION_ENTITY_TYPE],
  meta: {
    label: "Move to recycle bin",
  },
  conditions: [
    {
      alias: UMB_COLLECTION_ALIAS_CONDITION,
      match: SUBMISSIONS_COLLECTION_ALIAS,
    },
  ],
};

const SubmissionTrashEntityAction: ManifestEntityAction = {
  type: "entityAction",
  kind: "default",
  alias: "sproutForms.entityActions.submission.trash",
  name: "Move Submission To Recycle Bin",
  weight: 100,
  api: TrashSubmissionEntityAction,
  forEntityTypes: [SUBMISSION_ENTITY_TYPE],
  meta: {
    icon: "icon-trash",
    label: "Move to recycle bin",
  },
};

const SubmissionsRecycleBinRepositoryManifest: ManifestRepository = {
  type: "repository",
  alias: "sproutForms.repositories.submissions.recycleBin",
  name: "SproutForms Submissions Recycle Bin Repository",
  api: TrashedFormSubmissionsRepository,
};

const SubmissionsRecycleBinCollection: ManifestCollection = {
  type: "collection",
  kind: "default",
  alias: SUBMISSIONS_RECYCLE_BIN_COLLECTION_ALIAS,
  name: "SproutForms Submissions Recycle Bin Collection",
  api: SproutFormSubmissionsListContext,
  meta: {
    repositoryAlias: "sproutForms.repositories.submissions.recycleBin",
  },
};

const SubmissionsRecycleBinCollectionView: ManifestCollectionView = {
  type: "collectionView",
  alias: `${SUBMISSIONS_RECYCLE_BIN_COLLECTION_ALIAS}.overview`,
  name: "SproutForms Submissions Recycle Bin Overview",
  js: FormSubmissionCollectionElement,
  meta: {
    label: "Overview",
    icon: "icon-list",
    pathName: "overview",
  },
  conditions: [
    {
      alias: UMB_COLLECTION_ALIAS_CONDITION,
      match: SUBMISSIONS_RECYCLE_BIN_COLLECTION_ALIAS,
    },
  ],
};

const SubmissionsRecycleBinEmptyAction: ManifestCollectionAction = {
  type: "collectionAction",
  kind: "button",
  alias: `${SUBMISSIONS_RECYCLE_BIN_COLLECTION_ALIAS}.emptyAction`,
  name: "Empty SproutForms Submissions Recycle Bin",
  api: EmptySubmissionsRecycleBinAction,
  meta: {
    label: "Empty recycle bin",
  },
  conditions: [
    {
      alias: UMB_COLLECTION_ALIAS_CONDITION,
      match: SUBMISSIONS_RECYCLE_BIN_COLLECTION_ALIAS,
    },
  ],
};

const SubmissionsRecycleBinRestoreBulkAction: ManifestEntityBulkAction = {
  type: "entityBulkAction",
  alias: `${SUBMISSIONS_RECYCLE_BIN_COLLECTION_ALIAS}.restoreAction`,
  name: "Restore Submissions From Recycle Bin",
  weight: 20,
  api: RestoreSubmissionsBulkAction,
  forEntityTypes: [TRASHED_SUBMISSION_ENTITY_TYPE],
  meta: {
    label: "Restore",
  },
  conditions: [
    {
      alias: UMB_COLLECTION_ALIAS_CONDITION,
      match: SUBMISSIONS_RECYCLE_BIN_COLLECTION_ALIAS,
    },
  ],
};

const SubmissionsRecycleBinDeleteBulkAction: ManifestEntityBulkAction = {
  type: "entityBulkAction",
  alias: `${SUBMISSIONS_RECYCLE_BIN_COLLECTION_ALIAS}.deleteAction`,
  name: "Delete Submissions Permanently",
  weight: 10,
  api: DeleteSubmissionsPermanentlyBulkAction,
  forEntityTypes: [TRASHED_SUBMISSION_ENTITY_TYPE],
  meta: {
    label: "Delete permanently",
  },
  conditions: [
    {
      alias: UMB_COLLECTION_ALIAS_CONDITION,
      match: SUBMISSIONS_RECYCLE_BIN_COLLECTION_ALIAS,
    },
  ],
};

const TrashedSubmissionRestoreEntityAction: ManifestEntityAction = {
  type: "entityAction",
  kind: "default",
  alias: "sproutForms.entityActions.trashedSubmission.restore",
  name: "Restore Submission From Recycle Bin",
  weight: 200,
  api: RestoreSubmissionEntityAction,
  forEntityTypes: [TRASHED_SUBMISSION_ENTITY_TYPE],
  meta: {
    icon: "icon-undo",
    label: "Restore",
  },
};

const TrashedSubmissionDeleteEntityAction: ManifestEntityAction = {
  type: "entityAction",
  kind: "default",
  alias: "sproutForms.entityActions.trashedSubmission.delete",
  name: "Delete Submission Permanently",
  weight: 100,
  api: DeleteSubmissionPermanentlyEntityAction,
  forEntityTypes: [TRASHED_SUBMISSION_ENTITY_TYPE],
  meta: {
    icon: "icon-trash",
    label: "Delete permanently",
  },
};

const FormSubmissionInfoModal: ManifestModal = {
  type: "modal",
  alias: "sproutForms.modal.submission.info",
  name: "SproutForms Submission Info Modal",
  js: () => import("../modals/formSubmissionInfoModal.element"),
};

const FormTypePickerModal: ManifestModal = {
  type: "modal",
  alias: FORM_TYPE_PICKER_MODAL_ALIAS,
  name: "SproutForms Form Type Picker Modal",
  js: () => import("../modals/formTypePickerModal.element"),
};

const FormRollbackModal: ManifestModal = {
  type: "modal",
  alias: FORM_ROLLBACK_MODAL_ALIAS,
  name: "SproutForms Form Rollback Modal",
  js: () => import("../modals/formRollbackModal.element"),
};

const WorkflowTemplateModal: ManifestModal = {
  type: "modal",
  alias: WORKFLOW_TEMPLATE_MODAL_ALIAS,
  name: "SproutForms Workflow Template Modal",
  js: () => import("../modals/workflowTemplateModal.element"),
};

const FileFieldSubmissionField: SubmissionFieldManifest = {
  type: "submissionField",
  alias: "sproutForms.submissionField.file",
  name: "File Field Submission Field",
  element: FileSubmissionFieldElement,
  fieldTypeAlias: "file",
};

const RepeaterFieldSubmissionField: SubmissionFieldManifest = {
  type: "submissionField",
  alias: "sproutForms.submissionField.repeater",
  name: "Repeater Field Submission Field",
  element: RepeaterSubmissionFieldElement,
  fieldTypeAlias: "repeater",
};

const KeyValuePairFieldConfigProperty: FormFieldConfigManifest = {
  type: "formFieldConfig",
  alias: "sproutForms.fieldConfig.keyValuePair",
  name: "Key Value Pair Field Config Property",
  element: KeyValuePairProperty,
  propertyTypeAlias: "SproutForms.KeyValuePair",
};

const FieldOptionPickerFieldConfigProperty: FormFieldConfigManifest = {
  type: "formFieldConfig",
  alias: "sproutForms.fieldConfig.fieldOptionPicker",
  name: "Field Option Picker Field Config Property",
  element: FieldOptionPickerProperty,
  propertyTypeAlias: "SproutForms.FieldOptionPicker",
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
};

const TokenAutocompleteTextareaPropertyEditor: ManifestPropertyEditorUi = {
  type: 'propertyEditorUi',
  alias: 'sproutForms.propertyEditorUi.tokenTextarea',
  name: 'SproutForms Token Textarea',
  element: TokenAutocompleteTextareaElement,
  meta: {
    label: 'Token Textarea',
    icon: 'icon-script',
    group: 'common',
    propertyEditorSchemaAlias: 'Umbraco.Plain.String'
  }
};

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
  FormTrashEntityAction,
  RecycleBinWorkspace,
  RecycleBinCollection,
  RecycleBinCollectionView,
  RecycleBinRepositoryManifest,
  RecycleBinEmptyAction,
  RecycleBinRestoreBulkAction,
  RecycleBinDeleteBulkAction,
  TrashedFormRestoreEntityAction,
  TrashedFormDeleteEntityAction,
  FormSubmissionsRepositoryManifest,
  FormSubmissionsCollectionManifest,
  FormSubmissionsCollectionViewManifest,
  SubmissionTrashBulkAction,
  SubmissionTrashEntityAction,
  SubmissionsRecycleBinRepositoryManifest,
  SubmissionsRecycleBinCollection,
  SubmissionsRecycleBinCollectionView,
  SubmissionsRecycleBinEmptyAction,
  SubmissionsRecycleBinRestoreBulkAction,
  SubmissionsRecycleBinDeleteBulkAction,
  TrashedSubmissionRestoreEntityAction,
  TrashedSubmissionDeleteEntityAction,
  FormSubmissionInfoModal,
  FormTypePickerModal,
  FormRollbackModal,
  WorkflowTemplateModal,
  FileFieldSubmissionField,
  RepeaterFieldSubmissionField,
  KeyValuePairFieldConfigProperty,
  FieldOptionPickerFieldConfigProperty,
  FormsPickerPropertyEditor,
  TokenAutocompleteTextareaPropertyEditor
];
