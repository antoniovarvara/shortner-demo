export class BackendApiResponse {
    resourceId : string;
    completeUrl: string;

    
    constructor(resourceId: string, completeUrl: string){
        this.resourceId = resourceId;
        this.completeUrl = completeUrl;
    }

}