import { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { BackofficeSproutForms, CreateFolderRequest, FormBackofficeModel, WorkflowTemplateBackofficeModel } from "../api";

export class SproutFormsSource {
     #host: UmbControllerHost;

  constructor(host: UmbControllerHost) {
    this.#host = host;
  }

  async getForms(take: number, skip: number, parentId?: string | null){
    // Use tree endpoints which include both folders and forms
    const treeData = parentId 
      ? await tryExecute(this.#host, BackofficeSproutForms.getUmbracoSproutFormsChildren({
          query: { parentUnique: parentId, skip, take }
        }))
      : await tryExecute(this.#host, BackofficeSproutForms.getUmbracoSproutFormsRoot({
          query: { skip, take }
        }));
    
    return treeData;
  }

  async getSubmissions(take: number, skip: number, formId: string) {
    return await tryExecute(this.#host, BackofficeSproutForms.getUmbracoSproutFormsSubmissions({
      query: {
        skip,
        take,
        formId
      }
    }))
  } 

  async getSubmission(submissionId: string) {
    return await tryExecute(this.#host, BackofficeSproutForms.getUmbracoSproutFormsSubmission({ query: { submissionId } }));
  }

  async retryWorkflow(submissionId: string, workflowAlias: string) {
    return await tryExecute(this.#host, BackofficeSproutForms.postUmbracoSproutFormsSubmissionWorkflowRetry({
      query: { submissionId, workflowAlias }
    }));
  }

  async approveWorkflow(submissionId: string, workflowAlias: string) {
    return await tryExecute(this.#host, BackofficeSproutForms.postUmbracoSproutFormsSubmissionWorkflowApprove({
      query: { submissionId, workflowAlias }
    }));
  }

  async declineWorkflow(submissionId: string, workflowAlias: string) {
    return await tryExecute(this.#host, BackofficeSproutForms.postUmbracoSproutFormsSubmissionWorkflowDecline({
      query: { submissionId, workflowAlias }
    }));
  }

  async getFieldTypes() {
    return await tryExecute(this.#host, BackofficeSproutForms.getUmbracoSproutFormsFieldTypes())
  }

  async getOutcomes() {
    return await tryExecute(this.#host, BackofficeSproutForms.getUmbracoSproutFormsOutcomeTypes());
  }

  async getWorkflowTypes(){
    return await tryExecute(this.#host, BackofficeSproutForms.getUmbracoSproutFormsWorkflowTypes());
  }

  async getForm(formId: string) {
    return await tryExecute(this.#host, BackofficeSproutForms.getUmbracoSproutFormsForm({ query: { id: formId } }));
  }

  async saveForm(form: FormBackofficeModel){
    return await tryExecute(this.#host, BackofficeSproutForms.postUmbracoSproutFormsForm({
      body: form
    }))
  }

  async deleteForms(formIds: string[]){
    return await tryExecute(this.#host, BackofficeSproutForms.deleteUmbracoSproutFormsForm({
      body: formIds
    }))
  }

  async generateAlias(name: string, formId?: string) {
    return await tryExecute(this.#host, BackofficeSproutForms.postUmbracoSproutFormsGenerateAlias({
      query: {
        id: formId,
        name: name
      }
    }))
  }

  async saveFolder(model: CreateFolderRequest){
    return await tryExecute(this.#host, BackofficeSproutForms.postUmbracoSproutFormsFolder({
      body: model
    }))
  }

  async getDashboardInfo(){
    return await tryExecute(this.#host, BackofficeSproutForms.getUmbracoSproutFormsDashboard());
  }

  async getTemplates(){
    return await tryExecute(this.#host, BackofficeSproutForms.getUmbracoSproutFormsTemplates());
  }

  async createTemplate(template: WorkflowTemplateBackofficeModel){
    return await tryExecute(this.#host, BackofficeSproutForms.postUmbracoSproutFormsTemplates({
      body: template
    }));
  }

  async updateTemplate(template: WorkflowTemplateBackofficeModel){
    return await tryExecute(this.#host, BackofficeSproutForms.putUmbracoSproutFormsTemplatesById({
      path: { id: template.id! },
      body: template
    }));
  }

  async deleteTemplate(id: string){
    return await tryExecute(this.#host, BackofficeSproutForms.deleteUmbracoSproutFormsTemplatesById({
      path: { id }
    }));
  }
}