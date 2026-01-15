import { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { BackofficeSproutForms, FormBackofficeModel } from "../api";

export class SproutFormsSource {
     #host: UmbControllerHost;

  constructor(host: UmbControllerHost) {
    this.#host = host;
  }

  async getForms(take: number, skip: number){
    return await tryExecute(this.#host, BackofficeSproutForms.getUmbracoSproutFormsForms({
      query: {
        skip,
        take
      }
    }))
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
}