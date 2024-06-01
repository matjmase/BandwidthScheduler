import { Component, OnInit, ViewChild } from '@angular/core';
import { ITeam } from '../models/db/ITeam';
import { TeamSelectorComponent } from '../commonControls/team-selector/team-selector.component';
import { AddRemoveTeamUserComponent } from './add-remove-team-user/add-remove-team-user.component';
import { EditTeamComponent } from './edit-team/edit-team.component';
import { RemoveTeamComponent } from './remove-team/remove-team.component';

@Component({
  selector: 'app-staff-builder',
  templateUrl: './staff-builder.component.html',
  styleUrl: './staff-builder.component.scss',
})
export class StaffBuilderComponent {
  @ViewChild('editMemberControl')
  EditControl!: EditTeamComponent;
  @ViewChild('removeMemberControl')
  RemoveControl!: RemoveTeamComponent;
  @ViewChild('addRemoveMemberControl')
  AddRemoveControl!: AddRemoveTeamUserComponent;

  public TeamsUpdated(): void {
    this.EditControl.TeamCollectionUpdated();
    this.RemoveControl.TeamCollectionUpdated();
    this.AddRemoveControl.TeamCollectionUpdated();
  }
}
