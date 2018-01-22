#!/bin/bash -xe

ed <<"EOF" /etc/mock/default.cfg
$i

[copr-be.cloud.fedoraproject.org_results_managerforlustre_manager-for-lustre_epel-7-x86_64_]
name=added from: https://copr-be.cloud.fedoraproject.org/results/managerforlustre/manager-for-lustre/epel-7-x86_64/
baseurl=https://copr-be.cloud.fedoraproject.org/results/managerforlustre/manager-for-lustre/epel-7-x86_64/
enabled=1


.
w
q
EOF


MOCK_GID=$(getent group mock | cut -d: -f3)
useradd --gid $MOCK_GID mockbuild
usermod -a -G mock mockbuild
MOCK_UID=$(id -u mockbuild)
chown -R $MOCK_UID:$MOCK_GID /builddir

cd /builddir/
RELEASE=$(git rev-list HEAD | wc -l)

su - mockbuild <<EOF
set -xe
cd /builddir/
rpmbuild -bs --define epel\ 1 --define package_release\ $RELEASE --define _srcrpmdir\ \$PWD --define _sourcedir\ \$PWD *.spec
mock iml-device-scanner2-*.src.rpm -v --rpmbuild-opts="--define package_release\ $RELEASE"
EOF