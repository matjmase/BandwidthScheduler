import { ILoginCredentials } from './ILoginCredential';

export interface IRegisterCredentials extends ILoginCredentials {
  roles: string[];
}

export class RegisterCredentials implements IRegisterCredentials {
  email: string;
  password: string;
  roles: string[];

  constructor(formValues: any) {
    this.email = formValues.email;
    this.password = formValues.password;
    this.roles = [];

    const keys = Object.keys(formValues.roles);

    keys.forEach((element) => {
      if (formValues.roles[element] == true) {
        this.roles.push(element);
      }
    });
  }
}
